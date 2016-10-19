using UnityEngine;
using System.Collections;
using UnityEngine.Networking;

public class camera_control : MonoBehaviour {
	public byte rotation_speed=70;
	public byte speed=20;
	public GameObject FPSController;
	public GameObject fpscontroller_pref;

	void LateUpdate () {
		if (Input.GetKey("w")) transform.Translate(Vector3.forward*Time.deltaTime*speed,Space.Self);
		if (Input.GetKey("s")) transform.Translate(Vector3.back*Time.deltaTime*speed,Space.Self);
		if (Input.GetKey("d")) transform.Translate(Vector3.right*Time.deltaTime*speed,Space.Self);
		if (Input.GetKey("a")) transform.Translate(Vector3.left*Time.deltaTime*speed,Space.Self);

		if (Input.GetMouseButton(2)) 
		{
			transform.Rotate(Vector3.up*Time.deltaTime*rotation_speed*Input.GetAxis("Mouse X"),Space.World);
			transform.Rotate(Vector3.left*Time.deltaTime*rotation_speed*Input.GetAxis("Mouse Y"),Space.Self);
		}

		if (Input.GetKey(KeyCode.Space)) transform.Translate(Vector3.up*rotation_speed*Time.deltaTime);
		if (Input.GetKey(KeyCode.LeftShift)) transform.Translate(Vector3.down*rotation_speed*Time.deltaTime);

		if (Input.GetKeyDown("f")) 
		{
			if (FPSController!=null)
			{
				RaycastHit rh;
				int layerMask=1<<8;
				if (Physics.Raycast(transform.position,transform.forward,out rh,10000,layerMask))
				{
					FPSController.transform.position=rh.point+Vector3.up;
					FPSController.transform.rotation=transform.rotation;
					FPSController.SetActive(true);
					gameObject.SetActive(false);
				}
			}
			else 
			{
				RaycastHit rh;
				int layerMask=1<<8;
				if (Physics.Raycast(transform.position,transform.forward,out rh,10000,layerMask))
				{
					FPSController=Instantiate(fpscontroller_pref,rh.point,transform.rotation) as GameObject;
					FPSController.GetComponent<FPS_Controller>().outcam=gameObject;
					gameObject.SetActive(false);
				}
			}
		}
			
	}







}