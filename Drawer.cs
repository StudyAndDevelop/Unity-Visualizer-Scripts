using UnityEngine;
using System.Collections;
using System.IO;

public class Drawer : MonoBehaviour {

	const int chunk_size=64;
	public int height_coefficient=100;
	int matrix_size_x=1;
	int matrix_size_y=1;

	string matrix_x_s;
	string matrix_y_s;
	string folder_name="";
	string hc_string; //height_coefficient

	public Material normal_material;
	public Material vertex_color_material;
	public Material grid_material;
	public GameObject cam;

	GameObject[] existing_chunks;
	float average_height=0;

	bool genered=false;
	bool color_settings=false;
	bool grid=false;
	bool vertex_coloring=false;
	int k=16;

	int max_attitude=100;
	int min_attitude=0;

	string enter_string1; //рабочие буферные строки для введения данных
	string enter_string2;

	void Start () {
		k=Screen.height/9;
		matrix_x_s=matrix_size_x.ToString();
		matrix_y_s=matrix_size_y.ToString();
		folder_name=Application.dataPath+"/";
		hc_string=height_coefficient.ToString();

		if (PlayerPrefs.HasKey("previous_folder_name")) 
		{
			folder_name=PlayerPrefs.GetString("previous_folder_name");
		}
		existing_chunks=new GameObject[0];
	}		

	GameObject LoadChunk (string path) {
		if (!File.Exists(path)) {return (null);print("no chunk");}
		string dstring="";
		int readpos=0; //положение считывающего курсора
		int ix=0;
		int iy=0;
		float[,] vertex_array=new float[chunk_size,chunk_size]; //сетка высот
		int wp=0; //целая часть
		int pp=0; // дробная часть
		int it=-1; // индекс точки
		int ip=-1; //индекс пробела
		int s_length=0; // длина строки
		bool json_reading=false;
		try 
		{
			using (StreamReader sr = new StreamReader(path)) 
			{
				if (Path.GetExtension(path)==".json") 
				{
					json_reading=true;
					for (byte ji=0;ji<10;ji++) 
					{
						dstring=sr.ReadLine(); //по идее здесь должна быть обработка вложенных данных
					}
				}
				while (sr.Peek()>=0&&ix<chunk_size) 
				{ 
					dstring=sr.ReadLine();
					s_length=dstring.Length;
					if (dstring[s_length-2]==' ') {s_length--;dstring=dstring.Substring(0,s_length);} //отрезаем последний пробел
					if (s_length==1) {ix=chunk_size;break;} //если это последняя строка со скобкой, то выходим
					while (readpos<s_length&&iy<chunk_size) 
						{
						wp=0;pp=0;it=-1;ip=-1;
						if (!json_reading) it=dstring.IndexOf('.',readpos); else it=dstring.IndexOf(',',readpos);
						ip=dstring.IndexOf(' ',readpos); 
						if (ip==-1) 
						{
							if (it==-1) 
							{//последнее число без точки
								vertex_array[ix,iy]=int.Parse(dstring.Substring(readpos,s_length-readpos));
								iy++; //страховка
								readpos=s_length+1;
								break;
							}
							else 
							{//последнее число с точкой
								wp=int.Parse(dstring.Substring(readpos,it-readpos));
								pp=int.Parse(dstring.Substring(it+1,s_length-1-it));
								if (wp<0) pp*=-1;
								vertex_array[ix,iy]=wp+pp*Mathf.Pow(0.1f,s_length-1-it);
								iy++; //страховка
								readpos=s_length+1;
								break;
							}
						}
						else 
						{
							if (it>ip||it==-1) 
							{//целое число
								vertex_array[ix,iy]=int.Parse(dstring.Substring(readpos,ip-readpos));
								iy++;
								readpos=ip+1;
							}
							else 
							{
								wp=int.Parse(dstring.Substring(readpos,it-readpos));
								pp=int.Parse(dstring.Substring(it+1,ip-it-1));
								if (wp<0) pp*=-1;
								vertex_array[ix,iy]=wp+pp*Mathf.Pow(0.1f,s_length-1-it);
								iy++;
								readpos=ip+1;
							}
						}
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

		GameObject chunk=new GameObject("chunk");
		chunk.transform.position=Vector3.zero;
		MeshRenderer mr=chunk.AddComponent<MeshRenderer>();
		mr.material=normal_material;
		Mesh mesh=chunk.AddComponent<MeshFilter>().mesh;
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
				vertices[c] = new Vector3(j,vertex_array[i,j]*height_coefficient,chunk_size-i-1);
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
		mesh.vertices = vertices;
		mesh.triangles=triangles;
		mesh.uv=uvs;
		mesh.RecalculateNormals();
		mesh.RecalculateBounds();
		return (chunk);
	}

	void VertexPaint (GameObject chunk) 
	{
		if (!chunk.GetComponent<MeshFilter>()) return;
		Mesh m=chunk.GetComponent<MeshFilter>().mesh;
		MeshRenderer mr=chunk.GetComponent<MeshRenderer>();
		if (m==null||mr==null) return;

		float h=0;
		Color[] colors=new Color [m.vertices.Length];
		Color c_color=Color.white;
		for (int c=0;c<m.vertices.Length;c++) 
		{
			h=m.vertices[c].y;
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
		m.colors=colors;
		mr.material=vertex_color_material;
	}

	void CreateMatrix(string folder_address,int size_x,int size_y) 
	{
		//cleaning previous matrix
		if (existing_chunks.Length!=0) 	
			foreach (GameObject c in existing_chunks) 
			{
				if (c!=null) Destroy(c);
			}
		existing_chunks=new GameObject[size_x*size_y];


		//if (use_constant_address) path=constant_address+n;
		//else path=Application.dataPath+"/"+n;

		string path="";
		GameObject chunk;
		for (int i=0;i<size_x;i++) 
		{
			for (int j=0;j<size_y;j++) 
			{
				path=folder_address+"/Chunk ("+i.ToString()+","+j.ToString()+").json";
				chunk=LoadChunk(path);
				if (chunk!=null)	
				{
					existing_chunks[i*size_x+j]=chunk;
					existing_chunks[i*size_x+j].transform.position=new Vector3(i*chunk_size-1*i,0,j*chunk_size-1*j);
					//print ("chunk succesfully generated");
				}
			}
		}
	}

	void OnGUI() {
		if (!genered) {
			GUI.Label(new Rect(Screen.width/2-2*k,Screen.height/2-3*k,2*k,k),"Путь до папки с данными:");
			folder_name=GUI.TextField(new Rect(Screen.width/2-2*k,Screen.height/2-2*k,4*k,k),folder_name);
			GUI.Label(new Rect(Screen.width/2-2*k,Screen.height/2-k,2*k,k),"Размеры матрицы чанков:");
			matrix_x_s=GUI.TextField(new Rect(Screen.width/2-2*k,Screen.height/2,2*k,k),matrix_x_s);
			matrix_y_s=GUI.TextField(new Rect(Screen.width/2,Screen.height/2,2*k,k),matrix_y_s);

			GUI.Label(new Rect(Screen.width/2-2*k,Screen.height/2+2*k,2*k,k/2),"Коэффициент высоты");
			hc_string=GUI.TextField(new Rect(Screen.width/2-2*k,Screen.height/2+2.5f*k,k,k/2),hc_string);

			if (GUI.Button(new Rect(Screen.width/2-k,Screen.height/2+k,2*k,k),"Сгенерировать")) {
				PlayerPrefs.SetString("previous_folder_name",folder_name);
				int.TryParse(matrix_x_s,out matrix_size_x);
				int.TryParse(matrix_y_s,out matrix_size_y);
				int.TryParse(hc_string,out height_coefficient);
				CreateMatrix(folder_name,matrix_size_x,matrix_size_y);
				genered=true;
			}
		}
		else 
		{
			GUI.Label(new Rect(0,0,4*k,k),Application.dataPath);
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
				if (existing_chunks.Length!=0) 
				{
					foreach (GameObject c in existing_chunks) 	VertexPaint(c);
				}
				color_settings=false;
			}
			if (GUI.Button(new Rect(3*k,2*k,2*k,k/2),"Отключить")) 
			{
				vertex_coloring=false;
				if (existing_chunks.Length!=0) 
				{
					foreach (GameObject c in existing_chunks) 
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
				foreach (GameObject c in existing_chunks) {
				if (c!=null) 
				{
					Mesh m=c.GetComponent<MeshFilter>().mesh;
					GameObject[] g=new GameObject[1+(chunk_size-2)*2]; //frame and inner lines
						int ind=0;
						for (ind=0;ind<g.Length;ind++) g[ind]=new GameObject("line_renderer_basement");
						LineRenderer lr=g[0].AddComponent<LineRenderer>();
						LineRendererToGrid(lr,new Vector3[4] {m.vertices[0],m.vertices[chunk_size-1],m.vertices[chunk_size*chunk_size-1],m.vertices[chunk_size*(chunk_size-1)]});
						ind=1;
						int a=0;
						for (a=1;a<chunk_size-1;a++)  //horizontal
						{
							lr=g[ind].AddComponent<LineRenderer>();
							ind++;
							LineRendererToGrid(lr,new Vector3[2] {m.vertices[a*chunk_size],m.vertices[(a+1)*chunk_size-1]});
						}
						for (a=1;a<chunk_size-1;a++) //vertical
						{
							lr=g[ind].AddComponent<LineRenderer>();
							ind++;
							LineRendererToGrid(lr,new Vector3[2] {m.vertices[a],m.vertices[m.vertices.Length-chunk_size+a]});
						}
						for (ind=0;ind<g.Length;ind++) 
						{
							g[ind].transform.parent=c.transform;
							g[ind].name="grid"+ind.ToString();
						}
				}
				grid=false;
			}
			}
			if (GUI.Button(new Rect(2.5f*k,3*k,1.5f*k,k/2),"Отключить")) 
			{
				foreach (GameObject c in existing_chunks) 
				{
					if (c==null) continue;
					int i=0;
					while (true)  //ай-яй-яй!
					{
						Transform t=c.transform.FindChild("grid"+i.ToString());
						if (t!=null) Destroy(t.gameObject);
						else break;
					}
				}
			}
		}
	}

	void LineRendererToGrid (LineRenderer lr, Vector3[] points) {
		lr.SetWidth(0.1f,0.1f);
		lr.material=grid_material;
		lr.receiveShadows=false;
		lr.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
		lr.SetVertexCount(points.Length);
		lr.SetPositions(points);
	}
		
}


