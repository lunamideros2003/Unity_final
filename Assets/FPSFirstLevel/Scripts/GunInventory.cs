using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum MenuStyle{
	horizontal,vertical
}

public class GunInventory : MonoBehaviour {
	[Tooltip("Current weapon gameObject.")]
	public GameObject currentGun;
	private Animator currentHAndsAnimator;
	private int currentGunCounter = 0;

	[Tooltip("Put Strings of weapon objects from Resources Folder.")]
	public List<string> gunsIHave = new List<string>();

	[HideInInspector]
	public float switchWeaponCooldown;

	void Awake(){
		StartCoroutine("SpawnWeaponUponStart");

		if (gunsIHave.Count == 0)
			print ("No guns in the inventory");
	}

	IEnumerator SpawnWeaponUponStart(){
		yield return new WaitForSeconds (0.5f);
		StartCoroutine("Spawn",0);
	}

	void Update(){
		switchWeaponCooldown += 1 * Time.deltaTime;
		if(switchWeaponCooldown > 1.2f && Input.GetKey(KeyCode.LeftShift) == false){
			Create_Weapon();
		}
	}

	void Create_Weapon(){

		if(Input.GetKeyDown(KeyCode.UpArrow) || Input.GetAxis("Mouse ScrollWheel") > 0){
			switchWeaponCooldown = 0;
			currentGunCounter++;
			if(currentGunCounter > gunsIHave.Count-1){
				currentGunCounter = 0;
			}
			StartCoroutine("Spawn",currentGunCounter);
		}
		if(Input.GetKeyDown(KeyCode.DownArrow) || Input.GetAxis("Mouse ScrollWheel") < 0){
			switchWeaponCooldown = 0;
			currentGunCounter--;
			if(currentGunCounter < 0){
				currentGunCounter = gunsIHave.Count-1;
			}
			StartCoroutine("Spawn",currentGunCounter);
		}

		if(Input.GetKeyDown(KeyCode.Alpha1) && currentGunCounter != 0){
			switchWeaponCooldown = 0;
			currentGunCounter = 0;
			StartCoroutine("Spawn",currentGunCounter);
		}
		if(Input.GetKeyDown(KeyCode.Alpha2) && currentGunCounter != 1){
			switchWeaponCooldown = 0;
			currentGunCounter = 1;
			StartCoroutine("Spawn",currentGunCounter);
		}

	}

	IEnumerator Spawn(int _index){
		if (weaponChanging)
			weaponChanging.Play ();
		
		if(currentGun){
			currentHAndsAnimator.SetBool("changingWeapon", true);

			yield return new WaitForSeconds(0.8f);
			Destroy(currentGun);

			GameObject resource = (GameObject) Resources.Load(gunsIHave[_index].ToString());
			currentGun = (GameObject) Instantiate(resource, transform.position, Quaternion.identity);
			AssignHandsAnimator(currentGun);
		}
		else{
			GameObject resource = (GameObject) Resources.Load(gunsIHave[_index].ToString());
			currentGun = (GameObject) Instantiate(resource, transform.position, Quaternion.identity);
			AssignHandsAnimator(currentGun);
		}
	}

	void AssignHandsAnimator(GameObject _currentGun){
		if(_currentGun.name.Contains("Gun")){
			currentHAndsAnimator = currentGun.GetComponent<GunScript>().handsAnimator;
		}
	}

	public void DeadMethod(){
		Destroy (currentGun);
		Destroy (this);
	}

	[Header("Sounds")]
	public AudioSource weaponChanging;
}
