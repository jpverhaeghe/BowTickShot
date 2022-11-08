using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

// Reference for adding the score text particle coming out of the camera is from this Unity forum post:
// Is it possible to create Text string particles? - Unity Answers – answer by LiloE
// There is a bug with the particle seeing the other camera inside it that I have yet to figure out.
public class GameManager : MonoBehaviour
{
    // Wall Constants
    public const float ANGLE_ROTATION = 90;
    public const float NUM_ANGLES = (360 / ANGLE_ROTATION);
    public const int ENV_EDGE_THICKNESS = 8;        // the thickness of the environment to not place walls
    public const int CENTER_CLEAR_AREA = 5;         // the area around the player to keep clear of walls
    public const int MIN_NUM_OBSTACLES = 15;        // the minimum number of obstacles to put in
    public const int MAX_NUM_OBSTACLES = 40;        // the maximum number of obstacles to put in

    // Balloon constants
    private const float MIN_HEIGHT = 0.5f;          // minimum distance from the ground in meters
    private const float MAX_HEIGHT = 10;            // maximum distance from the ground in meters

    // Public variables used in this script
    public bool gameInProgress = false;             // tells elements of the game that game has started
    public int balloonInPlay = 0;                   // tells the game manager how many balloons are in play

    // Serialized variables to show in the Unity editor but not public to other scripts
    [Header("Wall generation data")]
    [SerializeField] GameObject floor;              // game object to access the floor object for spacing, etc.
    [SerializeField] GameObject[] wallObjects;      // an array of possible cubicle walls to place in the scene

    [Header("Balloon generation data")]
    [SerializeField] GameObject player;             // a link to the player object to spawn balloons close to them for now, may be used for more later
    [SerializeField] GameObject balloonPrefab;      // the balloon prefab to randomize in the world
    [SerializeField] int maxNumBallonsInPlay = 5;   // the maximum number of balloons that can be in play at one time

    [Header("UI Elements to update")]
    [SerializeField] GameObject startMenu;          // a link to the start menu so we can turn it off
    [SerializeField] GameObject uiMenu;             // a link to the ui menu so we can turn it on/off
    [SerializeField] GameObject endMenu;            // a link to the end menu so we can turn it on

    [SerializeField] ParticleSystem scoreParticle;  // a particle to display the score so it is easier to see
    [SerializeField] TextMeshProUGUI scorePartText; // the link to the particle text so we can update it
    [SerializeField] TextMeshProUGUI scoreText;     // the link to the text on the UI so we can update it
    [SerializeField] TextMeshProUGUI arrowText;     // the link to the text on the UI so we can update it
    [SerializeField] TextMeshProUGUI highScoreText; // the link to the text on the end screen so we can update it

    [Header("In Game Music")]
    [SerializeField] AudioSource backgroundMusic;

    // Private variables used in this script
    private Vector3 floorSize;                      // used to keep track of the floor area to place items in the world
    private int score = 0;                          // keeps track of player score - display at end
    private int numArrows = 20;                     // keeps track of the number of arrows the player has, if 0 game is over

    /// <summary>
    /// Start is called before the first frame update
    /// </summary>
    void Start()
    {
        // store the floor size as we use it for placing balloons, etc.
        floorSize = floor.GetComponent<MeshCollider>().bounds.size;

        // set up the walls in the arena (doing this here as it should be behind the canvas and want it to happen while 
        // the player is making the choice to play (similar to a load screen) - in case it takes a minute
        CreateWalls();

    } // end Start

    /// <summary>
    /// Update is called once per frame
    /// </summary>
    void Update()
    {
        // Spawns a balloon if it is needed
        SpawnBalloon();

    } // end Update

    /// <summary>
    /// Starts the game when it is time
    /// </summary>
    public void StartGame()
    {
        // turn off the title screen and turn on the score text and player
        startMenu.SetActive(false);
        uiMenu.SetActive(true);
        player.SetActive(true);

        // Setup initial balloon object and score
        SpawnBalloon();
        UpdateScore(0);
        UpdateArrows(0);

        // start the music
        backgroundMusic.Play();

        gameInProgress = true;
    }

    /// <summary>
    /// Displays the end game and restart and quit buttons - quits the game if selected or reloads the scene if not
    /// </summary>
    public void EndGame()
    {
        // End game
        gameInProgress = false;

        // make it so the player can't move
        player.SetActive(false);

        // update the high score text
        highScoreText.text = score + " points";

        // stop the music
        backgroundMusic.Stop();

        // starting a coroutine to put a delay in so game over screen doesn't seem too abrupt
        StartCoroutine("DelayEnd");

    } // EndGame

    /// <summary>
    /// When the restart button is clicked, restarts the game
    /// </summary>
    public void RestartGame()
    {
        // If the restart button clicked, just reload the scene to restart the game
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);

    } // RestartGame

    /// <summary>
    /// Updates the score based on the initial score value and lets the system know to spawn another
    /// </summary>
    /// <param name="scoreValue"></param>
    public void UpdateScore(int scoreValue)
    {
        score += scoreValue;

        // send out a particle so player can easily see how much they scored (only do this if the score was positive)
        if (scoreValue > 0)
        {
            scorePartText.text = scoreValue.ToString();
            scoreParticle.Play();
        }

        // update the UI with the new score
        scoreText.text = "Score: " + score.ToString();

    } // end UpdateScore

    /// <summary>
    /// Adds the specified number of arrows to the game, ends the game if arrows ever becomes 0
    /// </summary>
    /// <param name="numToAdd"></param>
    public void UpdateArrows(int numToAdd)
    {
        // add the number of arrows and update the text on screen
        numArrows += numToAdd;
        arrowText.text = "Arrows Left: " + numArrows.ToString();

        // if the player runs out of arrows, game is over
        if (numArrows <= 0)
        {
            EndGame();
        }

    } // end UpdateArrows

    /// <summary>
    /// Delays the end of the game so players can see the score before going to end screen
    /// </summary>
    /// <returns></returns>
    private IEnumerator DelayEnd()
    {
        // wait a bit before launching game over screen and turning of the UI
        yield return new WaitForSeconds(1);
        uiMenu.SetActive(false);
        endMenu.SetActive(true);

    } // end DelayEnd

    /// <summary>
    /// Create some walls so we have things to shoot around
    /// </summary>
    private void CreateWalls()
    {
        // get the offsets of the width as the floor is centered on the world origin
        float spawnOffsetX = (floorSize.x / 2) - ENV_EDGE_THICKNESS;
        float spawnOffsetZ = (floorSize.z / 2) - ENV_EDGE_THICKNESS;

        // choose a random number of obtacles based on pre-defined constants
        int numObstacles = Random.Range(MIN_NUM_OBSTACLES, MAX_NUM_OBSTACLES);

        // Generate walls on the grid determined above.
        for (int obstacle = 0; obstacle < numObstacles; obstacle++)
        {
            // grab a random wall index (adding wall density to add random holes to the area)
            int wallIndex = (int)Random.Range(0, wallObjects.Length);

            // get a random position in the area in X
            float spawnXPos = Random.Range(-spawnOffsetX, spawnOffsetX);

            // we want to skip the inner part of the map around the player so there is room to start
            while ( (spawnXPos > -CENTER_CLEAR_AREA) && (spawnXPos < CENTER_CLEAR_AREA) )
            {
                spawnXPos = Random.Range(-spawnOffsetX, spawnOffsetX);
            }

            // get a random position in the area in Z
            float spawnZPos = Random.Range(-spawnOffsetZ, spawnOffsetZ);

            // we want to skip the inner part of the map around the player so there is room to start
            while ( (spawnZPos > -CENTER_CLEAR_AREA) && (spawnZPos < CENTER_CLEAR_AREA) )
            {
                spawnZPos = Random.Range(-spawnOffsetZ, spawnOffsetZ);
            }

            // change the position by creating a vector and using the offsets and row and col positions
            Vector3 placePosition = new Vector3(spawnXPos, 0, spawnZPos);

            // now add a random rotation on a 90 degree around y axis so all walls are not facing the same way
            Quaternion rotation = Quaternion.Euler(0, ( (int)Random.Range(0, NUM_ANGLES) ) * ANGLE_ROTATION, 0);
            Instantiate<GameObject>(wallObjects[wallIndex], placePosition, rotation);
        }

    } // end CreateWalls

    /// <summary>
    /// Spawns a random balloon in the game world if it is needed
    /// </summary>
    private void SpawnBalloon()
    {
        // only spawn a balloon if there isn't one on the field already
        if (balloonInPlay < maxNumBallonsInPlay)
        {
            // Spawn a balloon in a random location in the world
            // for the X use the floor size value in X without the mountain edges
            float spawnOffsetX = (floorSize.x / 2) - ENV_EDGE_THICKNESS;
            float spawnXPos = Random.Range(-spawnOffsetX, spawnOffsetX);

            // for the height, use pre-defined values for min and max
            float spawnYPos = Random.Range(MIN_HEIGHT, MAX_HEIGHT);

            // for the Z use the floor size value in X without the mountain edges
            float spawnOffsetZ = (floorSize.z / 2) - ENV_EDGE_THICKNESS;
            float spawnZPos = Random.Range(-spawnOffsetZ, spawnOffsetZ);

            Instantiate(balloonPrefab, new Vector3(spawnXPos, spawnYPos, spawnZPos), Quaternion.identity);

            // let the system know we've spawned a balloon (only one at a time for now)
            balloonInPlay++;
        }

    } // end SpawnBalloon

}
