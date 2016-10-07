using UnityEngine;
using System.Collections;
using System.IO;

public class Drawer : MonoBehaviour {

	int chunk_size_x=64;
	int chunk_size_y=64;
	string size_x_s;
	string size_y_s;
	string constant_address="";
	bool use_constant_address=false;

	public Material normal_material;
	public Material vertex_color_material;
	public Material grid_material;
	public GameObject cam;
	float[,] ar;

	GameObject existing_chunk;
	float average_height=0;

	string file_name="ch3.json";
	bool genered=false;
	bool color_settings=false;
	bool grid=false;
	bool vertex_coloring=false;
	LineRenderer[] grid_lines;
	int grid_x=0;
	int grid_y=0;
	int k=16;

	int max_attitude=100;
	int min_attitude=0;

	string enter_string1; //рабочие буферные строки для введения данных
	string enter_string2;

	void Start () {
		k=Screen.height/9;
		size_x_s=chunk_size_x.ToString();
		size_y_s=chunk_size_y.ToString();
		if (PlayerPrefs.HasKey("constantAddress")) 
		{
			constant_address=PlayerPrefs.GetString("constantAddress");
			if (constant_address!="") use_constant_address=true;
		}
		if (PlayerPrefs.HasKey("lastFile")) file_name=PlayerPrefs.GetString("lastFile");
	}		

	public void ReadData (string n) {
		string path="";
		if (use_constant_address) path=constant_address+n;
			else path=Application.dataPath+"/"+n;
		string dstring="";
		int readpos=0; //положение считывающего курсора
		int ix=0;
		int iy=0;
		ar=new float[chunk_size_x,chunk_size_y];
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
				while (sr.Peek()>=0&&ix<chunk_size_x) 
				{ 
					dstring=sr.ReadLine();
					s_length=dstring.Length;
					if (dstring[s_length-2]==' ') {s_length--;dstring=dstring.Substring(0,s_length);} //отрезаем последний пробел
					if (s_length==1) {ix=chunk_size_x;break;} //если это последняя строка со скобкой, то выходим
					while (readpos<s_length&&iy<chunk_size_y) 
						{
						wp=0;pp=0;it=-1;ip=-1;
						if (!json_reading) it=dstring.IndexOf('.',readpos); else it=dstring.IndexOf(',',readpos);
						ip=dstring.IndexOf(' ',readpos); 
						if (ip==-1) 
						{
							if (it==-1) 
							{//последнее число без точки
								ar[ix,iy]=int.Parse(dstring.Substring(readpos,s_length-readpos));
								iy++; //страховка
								readpos=s_length+1;
								break;
							}
							else 
							{//последнее число с точкой
								wp=int.Parse(dstring.Substring(readpos,it-readpos));
								pp=int.Parse(dstring.Substring(it+1,s_length-1-it));
								if (wp<0) pp*=-1;
								ar[ix,iy]=wp+pp*Mathf.Pow(0.1f,s_length-1-it);
								iy++; //страховка
								readpos=s_length+1;
								break;
							}
						}
						else 
						{
							if (it>ip||it==-1) 
							{//целое число
								ar[ix,iy]=int.Parse(dstring.Substring(readpos,ip-readpos));
								iy++;
								readpos=ip+1;
							}
							else 
							{
								wp=int.Parse(dstring.Substring(readpos,it-readpos));
								pp=int.Parse(dstring.Substring(it+1,ip-it-1));
								if (wp<0) pp*=-1;
								ar[ix,iy]=wp+pp*Mathf.Pow(0.1f,s_length-1-it);
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

			MakeTerrain(ar,chunk_size_x,chunk_size_y);
		} 
		catch (IOException e)  
		{  
			print ("no data file");
		}  
	}


	public void MakeTerrain (float[,] array,int xsize, int ysize) {
		GameObject chunk=new GameObject("chunk");
		chunk.transform.position=Vector3.zero;
		MeshRenderer mr=chunk.AddComponent<MeshRenderer>();
		mr.material=normal_material;
		Mesh mesh=chunk.AddComponent<MeshFilter>().mesh;
		Vector3[] vertices = new Vector3[array.Length];
		Color[] colors=new Color[array.Length];
		Color c_color=Color.white; //current color
		Vector2[] uvs=new Vector2[array.Length];
		float sum_height=0;

		int c=0;
		for (int i=0;i<xsize;i++) 
		{
			for (int j=0;j<ysize;j++) 
			{
				vertices[c] = new Vector3(j,array[i,j],chunk_size_y-i-1);
				sum_height+=vertices[c].y;
				//print (vertices[c]);
				c++;
			}
		}
		int[] triangles=new int[xsize*ysize*6];
		c=0;

		for (int i=0;i<xsize-1;i++) 
		{
			for (int j=0;j<ysize-1;j++) 
			{  //  a b
				// c d
				triangles[c]=(i+1)*chunk_size_y+j; //c
				triangles[c+1]=i*chunk_size_y+j; //a
				triangles[c+2]=i*chunk_size_y+j+1; //b
				triangles[c+3]=(i+1)*chunk_size_y+j; //c
				triangles [c+4]=i*chunk_size_y+j+1; //b
				triangles[c+5]=(i+1)*chunk_size_y+j+1; //d
				c+=6;
			}
			}
		mesh.vertices = vertices;
		mesh.triangles=triangles;
		mesh.uv=uvs;
		mesh.RecalculateNormals();
		mesh.RecalculateBounds();
		if (existing_chunk) 
		{
			Destroy(existing_chunk);
			existing_chunk=chunk;
		}
		else 
		{
			existing_chunk=chunk;
		}
		average_height=sum_height/vertices.Length;
		cam.transform.position=existing_chunk.transform.position+new Vector3(-5,mesh.vertices[0].y+10,-5);
		cam.transform.LookAt(existing_chunk.transform.position+new Vector3(chunk_size_x,mesh.vertices[mesh.vertices.Length/2].y,chunk_size_y));
	}

	void VertexPaint () 
	{
		Mesh m=existing_chunk.GetComponent<MeshFilter>().mesh;
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
		existing_chunk.GetComponent<MeshRenderer>().material=vertex_color_material;
	}

	void OnGUI() {
		if (!genered) {
			GUI.Label(new Rect(Screen.width/2-2*k,Screen.height/2-3*k,2*k,k),"Имя файла в папке игры");
			file_name=GUI.TextField(new Rect(Screen.width/2-2*k,Screen.height/2-2*k,4*k,k),file_name);
			GUI.Label(new Rect(Screen.width/2-2*k,Screen.height/2-k,2*k,k),"Размеры чанка:");
			size_x_s=GUI.TextField(new Rect(Screen.width/2-2*k,Screen.height/2,2*k,k),size_x_s);
			size_y_s=GUI.TextField(new Rect(Screen.width/2,Screen.height/2,2*k,k),size_y_s);

			GUI.Label(new Rect(Screen.width/2-5*k,0,2*k,k*0.75f),"Адрес папки с файлами");
			constant_address=GUI.TextField(new Rect(Screen.width/2-3*k,0,6*k,k*0.75f),constant_address);
			if (GUI.Button(new Rect(Screen.width/2+3*k,0,2*k,0.75f*k),"Применить")) 
			{
				PlayerPrefs.SetString("constantAddress",constant_address);
				if (constant_address!="") use_constant_address=true;
				else use_constant_address=false;
			}

			if (GUI.Button(new Rect(Screen.width/2-k,Screen.height/2+k,2*k,k),"Сгенерировать")) {
				if (int.TryParse(size_x_s,out chunk_size_x)&&int.TryParse(size_y_s,out chunk_size_y)) 
				{
				genered=true;
					PlayerPrefs.SetString("lastFile",file_name);
				ReadData(file_name);
				}
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
				if (existing_chunk!=null) VertexPaint();
				color_settings=false;
			}
			if (GUI.Button(new Rect(3*k,2*k,2*k,k/2),"Отключить")) 
			{
				vertex_coloring=false;
				if (existing_chunk!=null) existing_chunk.GetComponent<MeshRenderer>().material=normal_material;
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
				enter_string1=grid_x.ToString();
				enter_string2=grid_y.ToString();
			}
		}
		if (grid) {
			GUI.Label(new Rect(k,2*k,2*k,k/2),"длина:");
			GUI.Label(new Rect(k,2.5f*k,2*k,k/2),"ширина:");
			enter_string1=GUI.TextField(new Rect(3*k,2*k,k,k/2),enter_string1);
			enter_string2=GUI.TextField(new Rect(3*k,2.5f*k,k,k/2),enter_string2);
			if (GUI.Button(new Rect(k,3*k,1.5f*k,k/2),"Включить")) 
			{
				int.TryParse(enter_string1,out grid_x);
				int.TryParse(enter_string2,out grid_y);
				if (grid_x>chunk_size_x) grid_x=chunk_size_x;
				if (grid_y>chunk_size_y) grid_y=chunk_size_y;
				if (existing_chunk&&grid_x>0&&grid_y>0) 
				{
					Mesh m=existing_chunk.GetComponent<MeshFilter>().mesh;
					GameObject g=null;
					grid_lines=new LineRenderer[grid_x+grid_y];
					for (int i=0;i<grid_x;i++) 
					{  
						g=new GameObject("lr"+i.ToString());
						grid_lines[i]=g.AddComponent<LineRenderer>();
						grid_lines[i].SetWidth(0.1f,0.1f);
						grid_lines[i].material=grid_material;
						grid_lines[i].receiveShadows=false;
						grid_lines[i].shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
						grid_lines[i].SetVertexCount(grid_x);
						for (int j=0;j<grid_y;j++) 
						{
							grid_lines[i].SetPosition(j,m.vertices[i*chunk_size_y+j]);
						}
					}
					for (int i=grid_x;i<grid_y+grid_x;i++) 
					{
						g=new GameObject("lr"+i.ToString());
						grid_lines[i]=g.AddComponent<LineRenderer>();
						grid_lines[i].SetWidth(0.1f,0.1f);
						grid_lines[i].material=grid_material;
						grid_lines[i].receiveShadows=false;
						grid_lines[i].shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
						grid_lines[i].SetVertexCount(grid_y);
						for (int j=0;j<grid_x;j++) 
						{
							grid_lines[i].SetPosition(j,m.vertices[j*chunk_size_y+i-grid_x]);
						}
					}
				}
				grid=false;
			}
			if (GUI.Button(new Rect(2.5f*k,3*k,1.5f*k,k/2),"Отключить")) 
			{
				foreach (LineRenderer lr in grid_lines) {Destroy(lr.gameObject);}
				grid_lines=new LineRenderer[0];
				grid=false;
			}
		}
	}
}


