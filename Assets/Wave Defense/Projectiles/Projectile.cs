using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Projectie
/// 
/// Nothing special at the moment besides requiring that a projectile 
/// incorporates RigidBody thereby giving it physical properties and 
/// collision detection. 
/// 
/// Note: OnCollisionEnter() the projectile should disappear but
///    at the moment the destroy() call seems to cause the objects
///    not to appear in the scene.
/// </summary>
[RequireComponent (typeof (Rigidbody))]
public class Projectile : MonoBehaviour {

	public List<string> destroyOnHitTags;
	
	void OnCollisionEnter(Collision collision){
		foreach (ContactPoint contact in collision.contacts) {
            Debug.DrawRay(contact.point, contact.normal, Color.white);
        }
		
		if (destroyOnHitTags.Contains(
				collision.gameObject.tag)){
			
			SendMessageUpwards("ObserveEvent", gameObject, SendMessageOptions.DontRequireReceiver);
			DestroyObject(gameObject);
		}


	}
	
}
