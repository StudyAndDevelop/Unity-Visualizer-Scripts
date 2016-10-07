using UnityEngine;
using System.Collections;
using UnityEngine.Networking;

public class camera_control : MonoBehaviour {
	public byte rotation_speed=70;
	public byte speed=20;


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
	}







}