using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Balloon : MonoBehaviour
{
    // Constant values used in this script
    public const float MIN_OSCILLATION_TIME = 0.5f;     // The minimum time to oscillate
    public const float MAX_OSCILLATION_TIME = 2.0f;     // The maximum time to oscillate
    public const float MIN_OSCILLATION_SPEED = 0.5f;    // The minimum speed to oscillate
    public const float MAX_OSCILLATION_SPEED = 2.0f;    // The maximum speed to oscillate
    public const float OSCILLATION_MULTIPLIER = 0.5f;   // The amount to multiply by for each axis oscillation

    // Public variables used in this script

    // Serialized variables to show in the Unity editor but not public to other scripts
    [Header("Balloon Data")]
    [SerializeField] Material[] balloonMaterials;       // links to different balloon materials to make them change a bit

    [Range(0f, 1f)]
    [SerializeField] float chanceOscillation = 0.5f;    // Percent chance of oscillation
    [Range(0f, 1f)]
    [SerializeField] float chanceXDirAdd = 0.25f;       // Percent chance of oscillation adding horizontal add (X)
    [Range(0f, 1f)]
    [SerializeField] float chanceXDirOnly = 0.5f;       // Percent chance of oscillation switching to horizontal (X)
    [Range(0f, 1f)]
    [SerializeField] float chanceZDirAdd = 0.25f;      // Percent chance of oscillation adding to horizontal (Z)
    [Range(0f, 1f)]
    [SerializeField] float chanceZDirOnly = 0.5f;       // Percent chance of oscillation switching to horizontal (Z)

    [Header("Audio Clips for Balloon")]
    [SerializeField] AudioSource balloonHit;

    // Private variables used in this script
    private GameManager gameManager;                    // a link to the Game Manager script so we can update score, etc.
    private Vector3 oscillationDir = Vector3.up;        // the direction to move, defaults to up but can be side to side
    private float oscillationTime = 0;                  // the time to oscillate in a direction
    private float oscillationSpeed = 0;                 // the speed to oscillate in a direction
    private float oscillationTimer = 0;                 // a timer to keep track of the oscillation
    private float baseValue = 1.0f;                     // The base score value of a balloon

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

        // Add random oscillization....
        if (Random.Range(0, 1) < chanceOscillation)
        {
            // randomize the time and speed of the oscillation
            oscillationTime = Random.Range(MIN_OSCILLATION_TIME, MAX_OSCILLATION_TIME);
            oscillationSpeed = Random.Range(MIN_OSCILLATION_SPEED, MAX_OSCILLATION_SPEED);

            // check for horizontal add or change on X axis
            float horizontalChance = Random.Range(0, 1);

            float valueIncrease = OSCILLATION_MULTIPLIER;

            if (horizontalChance < chanceXDirAdd)
            {
                oscillationDir += Vector3.right;
                valueIncrease += OSCILLATION_MULTIPLIER;
            }
            else if (horizontalChance < chanceXDirOnly)
            {
                oscillationDir = Vector3.right;
            }

            // check for horizontal add or change on Z axis
            horizontalChance = Random.Range(0, 1);

            if (horizontalChance < chanceZDirAdd)
            {
                oscillationDir += Vector3.forward;
                valueIncrease += OSCILLATION_MULTIPLIER;
            }
            else if (horizontalChance < chanceZDirOnly)
            {
                oscillationDir = Vector3.forward;

                // if it is only in Z direction, then value Increase is reset to 1
                valueIncrease = OSCILLATION_MULTIPLIER;
            }

            // increase the base value of the balloon score as it will be harder to hit
            baseValue += valueIncrease;
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
        
    } // end Update

    private void OnTriggerEnter(Collider other)
    {
        // When this balloon is hit by an arrow, increase the score and reset the number of arrows shot
        if (other.gameObject.CompareTag("Arrow") )
        {
            // use the distance from where the arrow was fired to get a multiplier for the score
            Vector3 startPos = other.gameObject.GetComponent<Arrow>().initialPos;
            Vector3 endPos = other.gameObject.transform.position;

            // score is based off distance
            float scoreMultiplier = Vector3.Distance(startPos, endPos);

            // then speed of balloon plus 1 to make sure it is at least one
            scoreMultiplier *= (1 + oscillationSpeed);

            // send down the base value (which was increased by motion as well) times the score multiplier
            gameManager.UpdateScore((int)(baseValue * scoreMultiplier) );

            // destr0y the balloon object (not destroying the arrow as it should go through the balloon)
            // Particle effect is destroying the balloon
            GetComponent<ParticleSystem>().Play();
            gameManager.balloonInPlay--;

            // play a popping sound
            balloonHit.Play();
        }

    } // end OnTriggerEnter
}
