using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Balloon : MonoBehaviour
{
    // Constant values used in this script
    public const float CHANCE_OSCILLATION = 0.75f;      // Percent chance of oscillation
    public const float MIN_OSCILLATION_TIME = 0.1f;     // The minimum time to oscillate
    public const float MAX_OSCILLATION_TIME = 2.5f;     // The maximum time to oscillate
    public const float MIN_OSCILLATION_SPEED = 0.5f;    // The minimum speed to oscillate
    public const float MAX_OSCILLATION_SPEED = 2.0f;    // The maximum speed to oscillate
    public const int BASE_VALUE = 1;                    // The base score value of a balloon

    // Public variables used in this script

    // Serialized variables to show in the Unity editor but not public to other scripts
    [Header("Balloon Data")]
    [SerializeField] Material[] balloonMaterials;       // links to different balloon materials to make them change a bit

    // Private variables used in this script
    private GameManager gameManager;                    // a link to the Game Manager script so we can update score, etc.
    private Vector3 oscillationDir = Vector3.up;        // the direction to move, defaults to up but can be side to side
    private float oscillationTime = 0;                  // the time to oscillate in a direction
    private float oscillationSpeed = 0;                 // the speed to oscillate in a direction
    private float oscillationTimer = 0;                 // a timer to keep track of the oscillation

    // Start is called before the first frame update
    void Start()
    {
        // Set a random colour for this balloon based on the number of materials we have added
        Material thisMaterial = balloonMaterials[Random.Range(0, balloonMaterials.Length)];
        GetComponent<Renderer>().material = thisMaterial;

        // set up the particle generator material as well (not sure why this isn't working - it should work - SetColor didn't work either)
        ParticleSystemRenderer ps = GetComponent<ParticleSystemRenderer>();
        ps.material = thisMaterial;

        // Get the game manager script from the GameManager object that is in the scene
        gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();

        // TODO: Add random oscillization....
        if (Random.Range(0, 1) < CHANCE_OSCILLATION)
        {
            oscillationTime = Random.Range(MIN_OSCILLATION_TIME, MAX_OSCILLATION_TIME);
            oscillationSpeed = Random.Range(MIN_OSCILLATION_SPEED, MAX_OSCILLATION_SPEED);
        }
        
    } // end Start

    // Update is called once per frame
    void Update()
    {
        // if the balloon is set to oscillate, the do so
        if (oscillationTime > 0)
        {
            oscillationTimer += Time.deltaTime;

            // if the oscillation timer is up, reverse direction
            if (oscillationTimer > oscillationTime)
            {
                oscillationTimer = 0;
                oscillationDir *= -1;
            }

            transform.Translate(oscillationDir * oscillationSpeed * Time.deltaTime);
        }
        
    }

    private void OnTriggerEnter(Collider other)
    {
        // When this balloon is hit by an arrow, increase the score and reset the number of arrows shot
        if (other.gameObject.CompareTag("Arrow") )
        {
            // use the distance from where the arrow was fired to get a multiplier for the score
            Vector3 startPos = other.gameObject.GetComponent<Arrow>().initialPos;
            Vector3 endPos = other.gameObject.transform.position;

            float scoreMultiplier = Vector3.Distance(startPos, endPos);
            gameManager.UpdateScore((int)(BASE_VALUE * scoreMultiplier * oscillationSpeed) );

            // destry the balloon object (not destroying the arrow as it should go through the balloon)
            // Particle effect is destroying the balloon
            //Destroy(gameObject); // testing to see if particle system destroys object as advertised
            GetComponent<ParticleSystem>().Play();
            gameManager.balloonInPlay = false;
        }
    }
}
