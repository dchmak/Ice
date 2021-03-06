/*
* Created by Daniel Mak
*/

using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour {

    [System.Serializable]
    public class Upgradable {
        public string name;
        public TextMeshProUGUI text;
        [Range(0f, 2f)] public float baseScaler;
        private int lv = 0;

        public void Upgrade() {
            lv++;
        }

        public int GetLV() {
            return lv;
        }

        public float GetScaler() {
            //print(name + Mathf.Pow(baseScaler, lv));
            return Mathf.Pow(baseScaler, lv);
        }
    }

    [Header("Initial Statistics")]
    [Range(0f, 10f)] public float movementSpeed;
    [Range(0f, 10f)] public float freezingMovementSpeed;
    [Range(0f, 10f)] public float miningMovementSpeed;
    [Range(0f, 100f)] public float maxHealth;

    [Header("Freeze settings")]
    public GameObject freezeCone;
    public Transform freezeGun;
    [Range(0f, 1f)] public float freezeGunCost;
    public GameObject freezeCirclePrefab;
    public float freezeCircleTime;
    [Range(0f, 100f)] public float freezeCircleCost;
    [Range(0f, 100f)] public float maxFreezePower;
    [Range(0f, 100f)] public float mineBonus;

    [Header("UI elements")]
    public Canvas canvas;
    public Image healthBar;
    public Image freezePowerBar;
    public GameObject powerupArrowPrefab;
    [Range(0f, 10f)] public float hidePowerupArrowRadius;
    [Range(0f, 100f)] public float powerupArrowRadius;

    [Header("Upgradable settings")]
    public Upgradable[] upgradables;

    private float health;
    private float freezePower;
    private bool isFreezing, isMining;
    private Vector2 movement;
    private Rigidbody2D rb;
    private GameObject powerupArrow;
    private AudioController audioController;

    public void TakeDamage(float damage) {
        if (health > 0) health -= damage;
    }

    public void Upgrade() {
        //print("Upgrading...");
        int upgradeItem = UnityEngine.Random.Range(0, upgradables.Length);
        upgradables[upgradeItem].Upgrade();
    }

    public void Recharge(float power) {
        freezePower += power;
        freezePower = Mathf.Clamp(freezePower, 0, maxFreezePower);
    }

    public void Recharge() {
        freezePower = maxFreezePower;
    } // to full

    public Upgradable FindUpgradable(string name) {
        return Array.Find(upgradables, upgradable => upgradable.name == name);
    }

    public void SetIsMining(bool mine) {
        isMining = mine;
    }

	private void Start () {
        audioController = AudioController.instance;

        rb = GetComponent<Rigidbody2D>();
        health = maxHealth;
        freezePower = maxFreezePower;

        powerupArrow = Instantiate(powerupArrowPrefab);
        powerupArrow.transform.SetParent(canvas.transform);

        freezeCone.GetComponent<ParticleSystem>().Stop();
    }
	
	private void Update () {
        // get WASD movement
        movement = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));

        // rotate to look at mouse
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Vector2 dir = ((Vector2)ray.origin - (Vector2)transform.position).normalized;
        float angle = -Mathf.Atan2(-dir.y, dir.x) * Mathf.Rad2Deg;

        transform.rotation = Quaternion.Euler(0, 0, angle);

        // freeze gun
        Upgradable costUpgrade = FindUpgradable("Cost");
        float upgradedGunCost = freezeGunCost * costUpgrade.GetScaler();
        float upgradeCircleCost = freezeCircleCost * costUpgrade.GetScaler();
        //print(upgradedGunCost);
        if (Input.GetMouseButtonDown(0) && freezePower > upgradedGunCost) { 
            freezeCone.GetComponent<ParticleSystem>().Play();
            isFreezing = true;
            audioController.Play("Freeze gun");
        } // start freezing
        if (Input.GetMouseButton(0) && isFreezing) {
            freezePower -= freezeGunCost;
        } // when using freeze gun
        if (Input.GetMouseButtonUp(0) || freezePower <= 0) {
            freezeCone.GetComponent<ParticleSystem>().Stop();
            isFreezing = false;
            audioController.Stop("Freeze gun");
        } // end freezing

        // freeze cone
        if (Input.GetMouseButtonUp(1) && freezePower > upgradeCircleCost) {
            //print((Vector2)ray.origin);
            audioController.Play("Freeze circle");
            freezePower -= freezeCircleCost;
            GameObject freezeCircle = Instantiate(freezeCirclePrefab, (Vector2)ray.origin, Quaternion.identity);
            Destroy(freezeCircle, freezeCircleTime);
        } // spawn freeze circle

        // health UI
        healthBar.fillAmount = health / maxHealth;
        canvas.transform.rotation = Quaternion.identity;
        //print(health);

        // freeze power bar
        freezePowerBar.fillAmount = freezePower / maxFreezePower;

        // update power up texts
        foreach (Upgradable upgradable in upgradables) {
            upgradable.text.text = "LV: " + upgradable.GetLV().ToString();
        }

        // death
        if (health <= 0) {
            SceneManager.LoadSceneAsync("Gameover");
            audioController.Stop();
            audioController.Play("Player death");
        }

        // mining sound
        if (isMining && !audioController.IsPlaying("Mine")) audioController.Play("Mine");
        if (!isMining && audioController.IsPlaying("Mine")) audioController.Stop("Mine");
    }

    private void FixedUpdate() {
        //print(movement);
        Upgradable speedUpgrade = FindUpgradable("Speed");

        if (isFreezing) rb.velocity = movement * freezingMovementSpeed * speedUpgrade.GetScaler();
        else if (isMining) rb.velocity = movement * miningMovementSpeed * speedUpgrade.GetScaler();
        else rb.velocity = movement * movementSpeed * speedUpgrade.GetScaler();
    }

    private void LateUpdate() {
        // get closest power up (null if no power up exist)
        Transform target = GetClosestObject(GameObject.FindGameObjectsWithTag("Power Up"));

        // if power up exist... 
        if (target != null) {
            Vector3 dir = target.position - transform.position;
            float distance = dir.magnitude;

            //print(distance);
            if (distance > hidePowerupArrowRadius) {
                powerupArrow.SetActive(true);
                powerupArrow.transform.localPosition = dir.normalized * powerupArrowRadius;

                float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                powerupArrow.transform.localRotation = Quaternion.Euler(0, 0, angle);
            } else powerupArrow.SetActive(false);
        }
    }

    private Transform GetClosestObject(GameObject[] obj) {
        Transform bestTarget = null;
        float closestDistanceSqr = Mathf.Infinity;
        Vector3 currentPosition = transform.position;

        foreach (GameObject potentialTarget in obj) {
            Vector3 directionToTarget = potentialTarget.transform.position - currentPosition;
            float dSqrToTarget = directionToTarget.sqrMagnitude;
            if (dSqrToTarget < closestDistanceSqr) {
                closestDistanceSqr = dSqrToTarget;
                bestTarget = potentialTarget.transform;
            }
        }

        return bestTarget;
    }
}