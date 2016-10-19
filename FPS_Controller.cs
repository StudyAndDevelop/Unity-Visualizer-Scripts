using UnityEngine;
using System.Collections;

public class FPS_Controller : MonoBehaviour {
	public GameObject outcam;
	public GameObject cam;
	public float speed=10;
	public float rotation_speed=30;

	int layerMask;
	RaycastHit rh;
	// Use this for initialization
	void Start () {
		layerMask=1<<8;
	}
	
	// Update is called once per frame
	void Update () 
	{
		if (!Physics.Raycast(transform.position,Vector3.down,out rh,1,layerMask))
		{
			transform.Translate(Vector3.down*9.8f*Time.deltaTime);
		}

		Vector3 mv=Vector3.zero;
		mv.x=Input.GetAxis("Horizontal")*speed*Time.deltaTime;
		mv.z=Input.GetAxis("Vertical")*speed*Time.deltaTime;
		transform.Translate(mv,Space.Self);

		mv=new Vector3(-1*Input.GetAxis("Mouse Y")*rotation_speed*Time.deltaTime,Input.GetAxis("Mouse X")*rotation_speed*Time.deltaTime,0);
		mv+=cam.transform.localRotation.eulerAngles;
		cam.transform.localRotation=Quaternion.Euler(mv);

		if (Input.GetKeyDown("f")&&outcam!=null) 
		{
			outcam.SetActive(true);
			gameObject.SetActive(false);
		}

	}
}
