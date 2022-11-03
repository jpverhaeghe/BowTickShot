using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    // Wall Constants
    public float WALL_DENSITY = 5f;                 // walls are too close together, the higher the number, the less walls
    public const float ANGLE_ROTATION = 90;
    public const float NUM_ANGLES = (360 / ANGLE_ROTATION);
    public const float WALL_WIDTH = 0.2f;           // probably want to grab this from the objects instead...just for speed today

    // Balloon constants
    private const int MIN_DIST_FROM_PLAYER = 10;    // minimum distance from the player in meters
    private const int MAX_DIST_FROM_PLAYER = 25;    // maximum distance from the player in meters
    private const float MIN_HEIGHT = 1.0f;          // minimum distance from the ground in meters
    private const float MAX_HEIGHT = 10;            // maximum distance from the ground in meters

    // Public variables used in this script
    public bool balloonInPlay = false;              // tells the game manager if it is ready to spawn a balloon
    public bool gameInProgress = false;             // tells elements of the game that game has started

    // Serialized variables to show in the Unity editor but not public to other scripts
    [Header("Wall generation data")]
    [SerializeField] GameObject floor;              // game object to access the floor object for spacing, etc.
    [SerializeField] GameObject[] wallObjects;      // an array of possible cubicle walls to place in the scene

    [Header("Balloon generation data")]
    [SerializeField] GameObject player;             // a link to the player object to spawn balloons close to them for now, may be used for more later
    [SerializeField] GameObject balloonPrefab;      // the balloon prefab to randomize in the world

    [Header("UI Elements to update")]
    [SerializeField] GameObject startMenu;          // a link to the start menu so we can turn it off
    [SerializeField] GameObject uiMenu;             // a link to the ui menu so we can turn it on/off
    [SerializeField] GameObject endMenu;            // a link to the end menu so we can turn it on

    [SerializeField] ParticleSystem scoreParticle;  // a particle to display the score so it is easier to see
    [SerializeField] TextMeshProUGUI scorePartText; // the link to the particle text so we can update it
    [SerializeField] TextMeshProUGUI scoreText;     // the link to the text on the UI so we can update it
    [SerializeField] TextMeshProUGUI arrowText;     // the link to the text on the UI so we can update it
    [SerializeField] TextMeshProUGUI highScoreText; // the link to the text on the end screen so we can update it

    // Private variables used in this script
    private GameObject[,] gameGrid;                 // a grid to contain walls, etc. in the scene.
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

        gameInProgress = true;
    }

    /// <summary>
    /// Displays the end game and restart and quit buttons - quits the game if selected or reloads the scene if not
    /// </summary>
    public void EndGame()
    {
        // TODO: add end game code here (there is more as we need end game screen, etc)
        gameInProgress = false;

        // make it so the player can't move
        player.SetActive(false);

        // update the high score text
        highScoreText.text = score + " points";

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
    /// TODO: Score is based on distance only, add bonus for curved arrow
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

    private IEnumerator DelayEnd()
    {
        // wait a bit before launching game over screen and turning of the UI
        yield return new WaitForSeconds(1);
        uiMenu.SetActive(false);
        endMenu.SetActive(true);

    }

    /// <summary>
    /// Create some walls so we have things to shoot around
    /// </summary>
    private void CreateWalls()
    {
        // set up the game grid for the cubicle walls...do we want to just have one per 1x1 meter grid space?
        // This works to get unit size of plane (planes are 10 x 10 for a single unit, so mayh need to multipy by local scale)
        // However, it makes it way too big and instantiating 1 million objects is not good - so need to make it smaller.
        // this was fixed by using Pro Builder to create the floor plane instead, now it is one to one as it isn't scaled

        // get the offsets of the width as the floor is centered on the world origin
        // TODO: Remove the edges from the area - need to make it feel more natural
        float floorXOffset = (floorSize.x / 2);
        float floorZOffset = (floorSize.z / 2);

        // The width/depth of the floor makes too many walls, so use a density value to make the number of walls less
        int arrayWidth = (int)(floorSize.x / WALL_DENSITY);
        int arrayHeight = (int)(floorSize.z / WALL_DENSITY);

        Debug.Log("Floor size is x: " + floorSize.x + " by z: " + floorSize.z);
        Debug.Log("Floor array size is x: " + arrayWidth + " by z: " + arrayHeight);

        gameGrid = new GameObject[arrayWidth, arrayHeight];

        // Generate walls on the grid determined above.
        for (int row = 0; row < arrayWidth; row++)
        {
            // make it so walls are further than 1 unit (1 meter) apart as that isn't much room doing it in the column for now
            for (int col = 0; col < arrayHeight; col += 2)
            {
                // grab a random wall index (adding one to add random holes to the area)
                int wallIndex = Random.Range(0, wallObjects.Length + 1);

                // if the index is in bounds of the wall array, we should instantiate a wall
                if (wallIndex < wallObjects.Length)
                {
                    // we want to skip the inner part of the map around the player so there is room to start
                    if ( ( (floorXOffset < -5) || (floorXOffset > 5) ) ||
                         ( (floorZOffset < -5) || (floorZOffset > 5) ) )
                    {

                        // change the position by creating a vector and using maths and row and col positions
                        Vector3 placePosition = new Vector3(wallObjects[wallIndex].transform.position.x + (row * WALL_DENSITY) - floorXOffset,
                                                            wallObjects[wallIndex].transform.position.y,
                                                            wallObjects[wallIndex].transform.position.z + (col * WALL_DENSITY) - floorZOffset);

                        // now add a random rotation on a 90 degree around y axis so all walls are not facing the same way
                        // TODO: May need to change how walls are drawn so they don't rotate on the middle point...TBD
                        Quaternion rotation = Quaternion.Euler(0, Random.Range(0, NUM_ANGLES) * ANGLE_ROTATION, 0);

                        gameGrid[row, col] = Instantiate<GameObject>(wallObjects[wallIndex], placePosition, rotation);
                    }
                }
            }
        }

    }

    /// <summary>
    /// Spawns a random balloon in the game world
    /// </summary>
    private void SpawnBalloon()
    {
        // only spawn a balloon if there isn't one on the field already
        if (!balloonInPlay)
        {
            // Spawn a balloon in a random location near the player (but not too close)
            // TODO: may need to be more clever in the future as there will be locations perhaps they can spawn at (may need a array of transforms)
            float spawnXPos = player.transform.position.x + getDistanceWithinTolerance();
            float spawnYPos = Random.Range(MIN_HEIGHT, MAX_HEIGHT);
            float spawnZPos = player.transform.position.z + getDistanceWithinTolerance();

            Instantiate(balloonPrefab, new Vector3(spawnXPos, spawnYPos, spawnZPos), Quaternion.identity);

            // let the system know we've spawned a balloon (only one at a time for now)
            balloonInPlay = true;
        }

    } // end SpawnBalloon

    /// <summary>
    /// Gets a distance within the given tolerances from the preset values in this script
    /// </summary>
    /// <returns>a float that is a distance within the set tolerances</returns>
    private float getDistanceWithinTolerance()
    {
        float dist = Random.Range(-MAX_DIST_FROM_PLAYER, MAX_DIST_FROM_PLAYER);

        // keep the balloon from getting too cloase to the player
        if (Mathf.Abs(dist) < MIN_DIST_FROM_PLAYER)
        {
            if (dist >= 0)
            {
                dist = MIN_DIST_FROM_PLAYER;
            }
            else
            {
                dist = -MIN_DIST_FROM_PLAYER;
            }
        }

        return dist;

    } // end getDistanceWithinTolerance
}
