using UnityEngine;
using System.Collections;
using System.IO;
using System.Collections.Generic;

public class Drawer : MonoBehaviour {
	// 1) Inspector-Input Objects	
	public Material normal_material;
	public Material vertex_color_material;
	public Material grid_material;
	public GameObject cam;
	// 2) Inspector-Input Technical Values
	const int chunk_size=64;
	public int height_coefficient=100;
	int matrix_radius=1;

	string matrix_radius_s;
	string folder_name="";
	string hc_string; //height_coefficient
	// 3) Inspector-Input Game Values
	// 4) Inspector-Input Textures and Sounds
	// 5) Script-using Objects
	public List <Chunk> playzone;
	// 6) Script-using Values
	bool genered=false;
	bool firstgen=true;
	bool color_settings=false;
	bool grid=false;
	bool vertex_coloring=false;

	int k=16;
	int max_attitude=100;
	int min_attitude=0;
	int player_grid_pos_x=0;
	int player_grid_pos_y=0;

	string enter_string1; //рабочие буферные строки для введения данных
	string enter_string2;
	// 7) Script-using Textures
	// 8) Scripts references
	// 9) Debugging variables

	void Start () {
		k=Screen.height/9;
		matrix_radius_s=matrix_radius.ToString();
		folder_name=Application.dataPath+"/";
		hc_string=height_coefficient.ToString();

		if (PlayerPrefs.HasKey("previous_folder_name")) 
		{
			folder_name=PlayerPrefs.GetString("previous_folder_name");
		}
	}		

	Chunk LoadChunk (string path) {
		if (!File.Exists(path)) {return (null);print("no chunk");}
		string dstring="";
		int readpos=0; //положение считывающего курсора
		int ix=0;
		int iy=0;
		float[,] vertex_array=new float[chunk_size,chunk_size]; //сетка высот
		int ip=-1; //индекс пробела
		Chunk chunk=new GameObject("chunk").AddComponent<Chunk>();
		try 
		{
			using (StreamReader sr = new StreamReader(path)) 
			{
				dstring=sr.ReadLine();
				if (dstring.Length>3)	chunk.ID=int.Parse(dstring.Substring(3));
				dstring=sr.ReadLine();
				if (dstring.Length>5)	chunk.grid_pos_x=int.Parse(sr.ReadLine().Substring(5));
				dstring=sr.ReadLine();
				if (dstring.Length>5)	chunk.grid_pos_y=int.Parse(sr.ReadLine().Substring(5));
				dstring=sr.ReadLine();
				if (dstring.Length>6)	chunk.SetBiome(sr.ReadLine().Substring(6));
				dstring=sr.ReadLine();
				if (dstring.Length>10)	chunk.BiomeName=sr.ReadLine().Substring(10);
				sr.ReadLine(); //skip "Cords"
				while (sr.Peek()>=0&&ix<chunk_size) 
				{ 
					dstring=sr.ReadLine();
					dstring=dstring.Replace(",",".");
					//print (dstring);
					if (dstring[dstring.Length-1]==' ') dstring=dstring.Substring(0,dstring.Length-1);
					if (dstring.Length==1) {ix=chunk_size;break;} //если это последняя строка со скобкой, то выходим
					while (readpos<dstring.Length&&iy<chunk_size) 
						{
						int a=-1;
						ip=dstring.IndexOf(' ',readpos); 
						if (ip!=-1) a=ip-readpos; 	else 	a=dstring.Length-readpos;
						if (!float.TryParse(dstring.Substring(readpos,a),out vertex_array[ix,iy])) {print (dstring.Substring(readpos,a));vertex_array[ix,iy]=-100;}
							//else print(vertex_array[ix,iy]);
							iy++;
							readpos+=a+1;
						}
					ix++;
					readpos=0;
					iy=0;
			}
			}
		} 
		catch (IOException e)  
		{  
			print ("cannot find file");
			return null;
		}  
			
		chunk.transform.position=Vector3.zero;
		chunk.mr=chunk.gameObject.AddComponent<MeshRenderer>();
		chunk.mr.material=normal_material;
		chunk.m=chunk.gameObject.AddComponent<MeshFilter>().mesh;
		Vector3[] vertices = new Vector3[vertex_array.Length];
		Color[] colors=new Color[vertex_array.Length];
		Color c_color=Color.white; //current color
		Vector2[] uvs=new Vector2[vertex_array.Length];
		float sum_height=0;

		int c=0;
		for (int i=0;i<chunk_size;i++) 
		{
			for (int j=0;j<chunk_size;j++) 
			{
				vertices[c] = new Vector3(j-chunk_size/2.0f,vertex_array[i,j]*height_coefficient,chunk_size/2.0f-i);
				sum_height+=vertices[c].y;
				c++;
			}
		}
		int[] triangles=new int[chunk_size*chunk_size*6];
		c=0;

		for (int i=0;i<chunk_size-1;i++) 
		{
			for (int j=0;j<chunk_size-1;j++) 
			{  //  a b
				// c d
				triangles[c]=(i+1)*chunk_size+j; //c
				triangles[c+1]=i*chunk_size+j; //a
				triangles[c+2]=i*chunk_size+j+1; //b
				triangles[c+3]=(i+1)*chunk_size+j; //c
				triangles [c+4]=i*chunk_size+j+1; //b
				triangles[c+5]=(i+1)*chunk_size+j+1; //d
				c+=6;
			}
			}
		chunk.m.vertices = vertices;
		chunk.m.triangles=triangles;
		chunk.m.uv=uvs;
		chunk.m.RecalculateNormals();
		chunk.m.RecalculateBounds();
		chunk.m.Optimize();
		return (chunk);
	}
		

	void VertexPaint (Chunk chunk) 
	{
		if (chunk.m==null||chunk.mr==null) return;
		float h=0;
		Color[] colors=new Color [chunk.m.vertices.Length];
		Color c_color=Color.white;
		for (int c=0;c<chunk.m.vertices.Length;c++) 
		{
			h=chunk.m.vertices[c].y;
			if (h>0) 
			{
				h/=max_attitude;
				h=1-h;
				c_color=Color.HSVToRGB(0+60*h/360,(1-h)*0.5f+0.5f,1);
			}
			else 
			{
				h/=max_attitude;
				h=1-h;
				c_color=Color.HSVToRGB(235/360f,h*0.5f+0.5f,1);
			}
			colors[c]=c_color;
		}
		chunk.m.colors=colors;
		chunk.mr.material=vertex_color_material;
	}

	void CreateMatrix(string folder_address,int radius) 
	{
		if (firstgen)
		{	
			if (playzone.Count>0)	{foreach (Chunk ch in playzone) Destroy(ch.gameObject);}
			playzone.Clear();
		//if (use_constant_address) path=constant_address+n;
		//else path=Application.dataPath+"/"+n;

		string path="";
		Chunk chunk;
		List<Vector2> chunks_numbers=GetBlocksInCirle(radius,Vector2.zero);
			if (radius==1) chunks_numbers=new List<Vector2>{Vector2.zero};

			foreach (Vector2 pos in chunks_numbers) 
			{
				//print (pos);
				path=folder_address+"Chunk ("+pos.x.ToString()+","+pos.y.ToString()+").json";
				chunk=LoadChunk(path);
				if (chunk!=null)	
				{
					chunk.gameObject.layer=8;
					chunk.gameObject.AddComponent<MeshCollider>();
					playzone.Add(chunk);
					chunk.transform.position=new Vector3(pos.x*chunk_size,0,pos.y*chunk_size);
				}
				else print("no chunk "+path);
			}	
		}
	}

	List<Vector2> GetBlocksInCirle(int radius, Vector2 pos)
	{
		Vector2 a,b,c,d;              									 // c d
		List<Vector2> blocks=new List<Vector2>();	 // a b
		int count=0;
		for (int x=(int)(pos.x-radius);x<=(int)(pos.x+radius);x++)
		{
			for (int y=(int)(pos.y-radius);y<=(int)(pos.y+radius);y++)
			{
				a=new Vector2(x-0.5f,y-0.5f); b=new Vector2(x+0.5f,y-0.5f); c=new Vector2(x-0.5f,y+0.5f);d=new Vector2(x+0.5f,y+0.5f);
				count=0;
				if (Vector2.Distance(a,pos)<radius) count++; 
				if (Vector2.Distance(b,pos)<radius) count++; 
				if (Vector2.Distance(c,pos)<radius) count++; 
				if (Vector2.Distance(d,pos)<radius) count++; 
				if (count>2) blocks.Add(a);
			}
		}
		return (blocks);
	}

	void OnGUI() {
		if (!genered) {
			GUI.Label(new Rect(Screen.width/2-2*k,Screen.height/2-3*k,2*k,k),"Путь до папки с данными:");
			folder_name=GUI.TextField(new Rect(Screen.width/2-2*k,Screen.height/2-2*k,4*k,k),folder_name);
			GUI.Label(new Rect(Screen.width/2-2*k,Screen.height/2-k,3*k,k),"Радиус игровой области ( в чанках):");
			matrix_radius_s=GUI.TextField(new Rect(Screen.width/2+k,Screen.height/2-k,k,k/2),matrix_radius_s);

			GUI.Label(new Rect(Screen.width/2-2*k,Screen.height/2,3*k,k),"Коэффициент высоты");
			hc_string=GUI.TextField(new Rect(Screen.width/2+k,Screen.height/2,k,k/2),hc_string);

			if (GUI.Button(new Rect(Screen.width/2-k,Screen.height/2+k,2*k,k),"Сгенерировать")) {
				PlayerPrefs.SetString("previous_folder_name",folder_name);
				int.TryParse(matrix_radius_s,out matrix_radius);
				int.TryParse(hc_string,out height_coefficient);
				CreateMatrix(folder_name,matrix_radius);
				genered=true;
			}
		}
		else 
		{
			if (GUI.Button(new Rect(Screen.width-4*k,0,2*k,k/2),"Перегенерировать")) genered=false;
			if (GUI.Button(new Rect(Screen.width-2*k,0,2*k,k/2),"Выход")) Application.Quit();
		}

		if (GUI.Button(new Rect(0,k,k,k),"Цвета")) 
		{
			if (color_settings) color_settings=false;
			else {
				color_settings=true;
				grid=false;
				enter_string1=max_attitude.ToString();
				enter_string2=min_attitude.ToString();
			}
		}
		if (color_settings) 
		{
			GUI.Label(new Rect(k,k,2*k,k/2),"max attitude");
			enter_string1=GUI.TextField(new Rect(3*k,k,2*k,k/2),enter_string1);
			GUI.Label(new Rect(k,1.5f*k,2*k,k/2),"min attitude");
			enter_string2=GUI.TextField(new Rect(3*k,1.5f*k,2*k,k/2),enter_string2);
			if (GUI.Button(new Rect(k,2*k,2*k,k/2),"Включить")) 
			{
				vertex_coloring=true;
				int.TryParse(enter_string1,out max_attitude);
				int.TryParse(enter_string2, out min_attitude);
				if (playzone.Count!=0) 
				{
					foreach (Chunk c in playzone) 	VertexPaint(c);
				}
				color_settings=false;
			}
			if (GUI.Button(new Rect(3*k,2*k,2*k,k/2),"Отключить")) 
			{
				vertex_coloring=false;
				if (playzone.Count!=0) 
				{
					foreach (Chunk c in playzone) 
					{
					c.GetComponent<MeshRenderer>().material=normal_material;
					}
				}
				color_settings=false;
			}
		}

		if (GUI.Button(new Rect(0,2*k,k,k),"Сетка")) 
		{
			if (grid) grid=false;
			else 
			{
				grid=true;
				color_settings=false;
			}
		}

		if (grid) {
			if (GUI.Button(new Rect(k,3*k,1.5f*k,k/2),"Включить")) 
			{
				foreach (Chunk c in playzone) {
				if (c!=null) 
				{
					Mesh m=c.GetComponent<MeshFilter>().mesh;
					GameObject[] g=new GameObject[2*chunk_size];
						int ind=0;
						for (ind=0;ind<g.Length;ind++) g[ind]=new GameObject("line_renderer_basement");
						LineRenderer lr;
						Vector3[] positions;
						ind=0;
						int a=0,b=0;
						for (a=0;a<chunk_size;a++)  //horizontal
						{
							g[ind].transform.parent=c.transform;
							g[ind].transform.localPosition=Vector3.zero;
							g[ind].name="grid"+ind.ToString();
							positions=new Vector3[chunk_size];
							for (b=0;b<chunk_size;b++)
							{
								positions[b]=m.vertices[a*chunk_size+b]+g[ind].transform.position;
							}
							lr=g[ind].AddComponent<LineRenderer>();
							LineRendererToGrid(ref lr,positions);
							ind++;
						}
						for (a=0;a<chunk_size;a++) //vertical
						{
							g[ind].transform.parent=c.transform;
							g[ind].transform.localPosition=Vector3.zero;
							g[ind].name="grid"+ind.ToString();
							positions=new Vector3[chunk_size];
							for (b=0;b<chunk_size;b++)
							{
								positions[b]=m.vertices[b*chunk_size+a]+g[ind].transform.position;
							}
							lr=g[ind].AddComponent<LineRenderer>();
							LineRendererToGrid(ref lr,positions);
							ind++;
						}
						for (ind=0;ind<g.Length;ind++) 
						{
							
						}
				}
				grid=false;
			}
			}
			if (GUI.Button(new Rect(2.5f*k,3*k,1.5f*k,k/2),"Отключить")) 
			{
				foreach (Chunk c in playzone) 
				{
					if (c==null) continue;
					int i=0;
					Transform t;
					for (i=0;i<c.transform.childCount;i++)
					{
						t=c.transform.GetChild(i);
						if (t.name.Substring(0,4)=="grid"&&t.gameObject.GetComponent<LineRenderer>()!=null) Destroy(t.gameObject);
					}
				}
			}
		}
	}

	void LineRendererToGrid (ref LineRenderer lr, Vector3[] points) {
		lr.SetWidth(0.1f,0.1f);
		lr.material=grid_material;
		lr.receiveShadows=false;
		lr.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
		lr.SetVertexCount(points.Length);
		lr.SetPositions(points);
	}
		
}


