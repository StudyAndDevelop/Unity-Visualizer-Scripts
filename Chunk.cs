using UnityEngine;
using System.Collections;

public class Chunk : MonoBehaviour {
	public Mesh m;
	public MeshRenderer mr;

	public int ID,grid_pos_x,grid_pos_y,size;
	public float temp,wet;
		public BiomeType Biome;
		public string BiomeName;

	public enum BiomeType
	{
		Desert,//Пустыня
		Savanna,//Савана
		Tropic,//Тропики
		Grassland,//Лужайка
		Wood,//Роща
		Forest,//Лес
		Tundra, //Тундра
		Ice, //Лёд
		Edge,
		Jarkovia //olololo
	}

	public void SetBiome (string s) 
	{
		if (s[0]==' ') s=s.Substring(1);
		if (s[s.Length-1]==' ') s=s.Substring(0,s.Length-2);
		switch (s)
		{
		case "Desert": Biome=BiomeType.Desert;break;
		case "Savanna": Biome=BiomeType.Savanna;break;
		case "Tropic":Biome=BiomeType.Tropic;break;
		case "Grassland": Biome=BiomeType.Grassland;break;
		case "Wood": Biome=BiomeType.Wood;break;
		case "Forest":Biome=BiomeType.Forest;break;
		case "Tundra": Biome=BiomeType.Tundra;break;
		case "Ice":Biome=BiomeType.Ice;break;
		case "Edge": Biome=BiomeType.Edge;break;
		default: Biome=BiomeType.Jarkovia;break;
		}
	}

}
