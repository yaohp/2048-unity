using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using static ImageCropper;
using UnityEngine.EventSystems;
using Button = UnityEngine.UI.Button;

public class GridManager : MonoBehaviour
{
    private static int rows = 4;
    private static int cols = 4;
    private static int lowestNewTileValue = 2;
    private static int highestNewTileValue = 4;
    private static float borderOffset = 0.05f / 2f;
    private static float horizontalSpacingOffset = -1.65f / 2f;
    private static float verticalSpacingOffset = 1.65f / 2f;
    private static float borderSpacing = 0.1f / 2f;
    private static float halfTileWidth = 0.55f / 2f;
    private static float spaceBetweenTiles = 1.1f / 2f;
    private static float tileW = 1f / 2f;
    private static float tileH = 1f / 2f;

    private int points;
    private List<GameObject> tiles;
    private Rect resetButton;
    private Rect gameOverButton;
    private Vector2 touchStartPosition = Vector2.zero;

    public int maxValue = 2048;
    public GameObject gameOverPanel;
    public GameObject noTile;
    public Text scoreText;
    public GameObject[] tilePrefabs;
    public LayerMask backgroundLayer;
    public float minSwipeDistance = 10.0f;

    private IEnumerator ReplaceImage(string path)
    {
        yield return Utils.LoadImage(path, callback);
    }

    private void callback(Texture2D texture2D)
    {
        //裁剪图片
        ImageCropper.Instance.Show(texture2D, (bool result, Texture originalImage, Texture2D croppedImage) =>
        {



            // If screenshot was cropped successfully
            if (result)
            {
                //保存裁剪后的png图片
                Utils.SavePng(croppedImage, CurrentButton.name);
                Sprite sprite = Sprite.Create(croppedImage, new Rect(0, 0, 100, 100), new Vector2(0.5f, 0.5f));
                //sr.sprite = sprite;
                CurrentButton.image.sprite = sprite;
                //设置已经显示的tile
                foreach (GameObject gameObject in tiles)
                {
                    Tile tile = gameObject.GetComponent<Tile>();
                    if (tile.value.ToString() == CurrentButton.name)
                    {
                        SpriteRenderer sr = gameObject.GetComponent<SpriteRenderer>();
                        sr.sprite = sprite;
                    }
                }
            }

            // Destroy the screenshot as we no longer need it in this case
            Destroy(texture2D);
        },
             settings: new ImageCropper.Settings()
             {
                 ovalSelection = false,//是否椭圆形
                 autoZoomEnabled = true,//自动缩放合适的区域
                 imageBackground = Color.clear, // transparent background
                 selectionMinSize = new Vector2(512, 512),
                 selectionMaxSize = new Vector2(4096, 4096),
                 selectionMinAspectRatio = 0,
                 selectionMaxAspectRatio = 0

             },
             croppedImageResizePolicy: (ref int width, ref int height) =>
             {
                 // 输出100*100像素的图片
                 width = 100;
                 height = 100;

             });
    }

    [SerializeField]
    private Kakera.Unimgpicker imagePicker;

    public GameObject ChangeImagePanel;
    public Button[] clickableImages;
    private Button CurrentButton;


    public void OnPressShowPicker(Button button)
    {
        CurrentButton = button;
        imagePicker.Show("Select Image", "unimgpicker");
    }
    public void OnPressShowChangeButton()
    {
        ChangeImagePanel.SetActive(true);
    }
    public void OnPressSaveChangeButton()
    {
        //Debug.Log("OnPressSaveChangeButton");
        ChangeImagePanel.SetActive(false);
    }
    private enum State
    {
        Loaded,
        WaitingForInput,
        CheckingMatches,
        GameOver
    }

    private State state;

    #region monodevelop
    void Awake()
    {
        tiles = new List<GameObject>();
        state = State.Loaded;
        imagePicker.Completed += (string path) =>
        {
            //Debug.Log("OnComplete:" + path);
            StartCoroutine(ReplaceImage(path));
        };
        foreach (Button button in clickableImages)
        {
            button.onClick.AddListener(delegate ()
            {
                //Debug.Log("click:" + button);
                OnPressShowPicker(button);

            });
            StartCoroutine(Utils.LoadImageByName(button.name, (texture) =>
            {
                if (texture != null)
                {
                    button.image.sprite = Sprite.Create(texture, new Rect(0, 0, 100, 100), new Vector2(0.5f, 0.5f));
                }
            }));
        }

    }

    void Update()
    {
        if (state == State.GameOver)
        {
            gameOverPanel.SetActive(true);
        }
        else if (state == State.Loaded)
        {
            state = State.WaitingForInput;
            GenerateRandomTile();
            GenerateRandomTile();
        }
        else if (state == State.WaitingForInput)
        {
#if UNITY_STANDALONE
            if (Input.GetButtonDown("Left"))
            {
                if (MoveTilesLeft())
                {
                    state = State.CheckingMatches;
                }
            }
            else if (Input.GetButtonDown("Right"))
            {
                if (MoveTilesRight())
                {
                    state = State.CheckingMatches;
                }
            }
            else if (Input.GetButtonDown("Up"))
            {
                if (MoveTilesUp())
                {
                    state = State.CheckingMatches;
                }
            }
            else if (Input.GetButtonDown("Down"))
            {
                if (MoveTilesDown())
                {
                    state = State.CheckingMatches;
                }
            }
            else if (Input.GetButtonDown("Reset"))
            {
                Reset();
            }
            else if (Input.GetButtonDown("Quit"))
            {
                Application.Quit();
            }
#endif

#if UNITY_ANDROID || UNITY_IOS || UNITY_WP8 || UNITY_WP8_1
            if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
            {
                touchStartPosition = Input.GetTouch(0).position;
            }
            else
            {
                //return;
            }
            if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Ended)
            {
                Vector2 swipeDelta = (Input.GetTouch(0).position - touchStartPosition);
                if (swipeDelta.magnitude < minSwipeDistance)
                {
                    return;
                }
                swipeDelta.Normalize();
                if (swipeDelta.y > 0.0f && swipeDelta.x > -0.5f && swipeDelta.x < 0.5f)
                {
                    if (MoveTilesUp())
                    {
                        state = State.CheckingMatches;
                    }
                }
                else if (swipeDelta.y < 0.0f && swipeDelta.x > -0.5f && swipeDelta.x < 0.5f)
                {
                    if (MoveTilesDown())
                    {
                        state = State.CheckingMatches;
                    }
                }
                else if (swipeDelta.x > 0.0f && swipeDelta.y > -0.5f && swipeDelta.y < 0.5f)
                {
                    if (MoveTilesRight())
                    {
                        state = State.CheckingMatches;
                    }
                }
                else if (swipeDelta.x < 0.0f && swipeDelta.y > -0.5f && swipeDelta.y < 0.5f)
                {
                    if (MoveTilesLeft())
                    {
                        state = State.CheckingMatches;
                    }
                }
            }
#endif
        }
        else if (state == State.CheckingMatches)
        {
            GenerateRandomTile();
            if (CheckForMovesLeft())
            {
                ReadyTilesForUpgrading();
                state = State.WaitingForInput;
            }
            else
            {
                state = State.GameOver;
            }
        }
    }
    #endregion

    #region class methods
    private static Vector2 GridToWorldPoint(int x, int y)
    {
        return new Vector2(x * tileW + horizontalSpacingOffset + borderSpacing * x,
                       -y * tileH + verticalSpacingOffset - borderSpacing * y);
    }

    private static Vector2 WorldToGridPoint(float x, float y)
    {
        return new Vector2((x * tileW - horizontalSpacingOffset) / (1 + borderSpacing),
                           (y * tileH - verticalSpacingOffset) / -(1 + borderSpacing));
    }
    #endregion

    #region private methods
    private bool CheckForMovesLeft()
    {
        if (tiles.Count < rows * cols)
        {
            return true;
        }

        for (int x = 0; x < cols; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                Tile currentTile = GetObjectAtGridPosition(x, y).GetComponent<Tile>();
                Tile rightTile = GetObjectAtGridPosition(x + 1, y).GetComponent<Tile>();
                Tile downTile = GetObjectAtGridPosition(x, y + 1).GetComponent<Tile>();

                if (x != cols - 1 && currentTile.value == rightTile.value)
                {
                    return true;
                }
                else if (y != rows - 1 && currentTile.value == downTile.value)
                {
                    return true;
                }
            }
        }
        return false;
    }

    public void GenerateRandomTile()
    {
        if (tiles.Count >= rows * cols)
        {
            throw new UnityException("Unable to create new tile - grid is already full");
        }

        int value;
        // find out if we are generating a tile with the lowest or highest value
        float highOrLowChance = Random.Range(0f, 0.99f);
        if (highOrLowChance >= 0.9f)
        {
            value = highestNewTileValue;
        }
        else
        {
            value = lowestNewTileValue;
        }

        // attempt to get the starting position
        int x = Random.Range(0, cols);
        int y = Random.Range(0, rows);

        // starting from the random starting position, loop through
        // each cell in the grid until we find an empty positio
        bool found = false;
        while (!found)
        {
            if (GetObjectAtGridPosition(x, y) == noTile)
            {
                found = true;
                Vector2 worldPosition = GridToWorldPoint(x, y);
                //     Debug.Log("x:"+x+"y:"+y+"pos:"+worldPosition);
                GameObject obj;
                if (value == lowestNewTileValue)
                {
                    obj = SimplePool.Spawn(tilePrefabs[0], worldPosition, transform.rotation);
                }
                else
                {
                    obj = SimplePool.Spawn(tilePrefabs[1], worldPosition, transform.rotation);
                }
                Tile tile = obj.GetComponent<Tile>();
                StartCoroutine(Utils.LoadImageByName(tile.value.ToString(), (texture) =>
                {
                    //Debug.Log("load:" + tile.value.ToString() + "===" + texture);
                    if (texture != null)
                    {
                        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, 100, 100), new Vector2(0.5f, 0.5f));
                        SpriteRenderer sr = obj.GetComponent<SpriteRenderer>();
                        sr.sprite = sprite;
                    }

                }));

                tiles.Add(obj);
                TileAnimationHandler tileAnimManager = obj.GetComponent<TileAnimationHandler>();
                tileAnimManager.AnimateEntry();
            }

            x++;
            if (x >= cols)
            {
                y++;
                x = 0;
            }

            if (y >= rows)
            {
                y = 0;
            }
        }
    }

    private GameObject GetObjectAtGridPosition(int x, int y)
    {
        RaycastHit2D hit = Physics2D.Raycast(GridToWorldPoint(x, y), Vector2.right, borderSpacing);

        if (hit && hit.collider.gameObject.GetComponent<Tile>() != null)
        {
            return hit.collider.gameObject;
        }
        else
        {
            return noTile;
        }
    }

    private bool MoveTilesDown()
    {
        bool hasMoved = false;
        for (int y = rows - 1; y >= 0; y--)
        {
            for (int x = 0; x < cols; x++)
            {
                GameObject obj = GetObjectAtGridPosition(x, y);

                if (obj == noTile)
                {
                    continue;
                }

                Vector2 raycastOrigin = obj.transform.position;
                raycastOrigin.y -= halfTileWidth;
                RaycastHit2D hit = Physics2D.Raycast(raycastOrigin, -Vector2.up, Mathf.Infinity);
                if (hit.collider != null)
                {
                    GameObject hitObject = hit.collider.gameObject;
                    if (hitObject != obj)
                    {
                        if (hitObject.tag == "Tile")
                        {
                            Tile thatTile = hitObject.GetComponent<Tile>();
                            Tile thisTile = obj.GetComponent<Tile>();
                            if (CanUpgrade(thisTile, thatTile))
                            {
                                UpgradeTile(obj, thisTile, hitObject, thatTile);
                                hasMoved = true;
                            }
                            else
                            {
                                Vector3 newPosition = hitObject.transform.position;
                                newPosition.y += spaceBetweenTiles;
                                if (!Mathf.Approximately(obj.transform.position.y, newPosition.y))
                                {
                                    obj.transform.position = newPosition;
                                    hasMoved = true;
                                }
                            }
                        }
                        else if (hitObject.tag == "Border")
                        {
                            Vector3 newPosition = obj.transform.position;
                            newPosition.y = hit.point.y + halfTileWidth + borderOffset;
                            if (!Mathf.Approximately(obj.transform.position.y, newPosition.y))
                            {
                                obj.transform.position = newPosition;
                                hasMoved = true;
                            }
                        }
                    }
                }
            }
        }

        return hasMoved;
    }

    private bool MoveTilesLeft()
    {
        bool hasMoved = false;
        for (int x = 1; x < cols; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                GameObject obj = GetObjectAtGridPosition(x, y);

                if (obj == noTile)
                {
                    continue;
                }

                Vector2 raycastOrigin = obj.transform.position;
                raycastOrigin.x -= halfTileWidth;
                RaycastHit2D hit = Physics2D.Raycast(raycastOrigin, -Vector2.right, Mathf.Infinity);
                if (hit.collider != null)
                {
                    GameObject hitObject = hit.collider.gameObject;
                    if (hitObject != obj)
                    {
                        if (hitObject.tag == "Tile")
                        {
                            Tile thatTile = hitObject.GetComponent<Tile>();
                            Tile thisTile = obj.GetComponent<Tile>();
                            if (CanUpgrade(thisTile, thatTile))
                            {
                                UpgradeTile(obj, thisTile, hitObject, thatTile);
                                hasMoved = true;
                            }
                            else
                            {
                                Vector3 newPosition = hitObject.transform.position;
                                newPosition.x += spaceBetweenTiles;
                                if (!Mathf.Approximately(obj.transform.position.x, newPosition.x))
                                {
                                    obj.transform.position = newPosition;
                                    hasMoved = true;
                                }
                            }
                        }
                        else if (hitObject.tag == "Border")
                        {
                            Vector3 newPosition = obj.transform.position;
                            newPosition.x = hit.point.x + halfTileWidth + borderOffset;
                            if (!Mathf.Approximately(obj.transform.position.x, newPosition.x))
                            {
                                obj.transform.position = newPosition;
                                hasMoved = true;
                            }
                        }
                    }
                }
            }
        }

        return hasMoved;
    }

    private bool MoveTilesRight()
    {
        bool hasMoved = false;
        for (int x = cols - 1; x >= 0; x--)
        {
            for (int y = 0; y < rows; y++)
            {
                GameObject obj = GetObjectAtGridPosition(x, y);

                if (obj == noTile)
                {
                    continue;
                }

                Vector2 raycastOrigin = obj.transform.position;
                raycastOrigin.x += halfTileWidth;
                RaycastHit2D hit = Physics2D.Raycast(raycastOrigin, Vector2.right, Mathf.Infinity);
                if (hit.collider != null)
                {
                    GameObject hitObject = hit.collider.gameObject;
                    if (hitObject != obj)
                    {
                        if (hitObject.tag == "Tile")
                        {
                            Tile thatTile = hitObject.GetComponent<Tile>();
                            Tile thisTile = obj.GetComponent<Tile>();
                            if (CanUpgrade(thisTile, thatTile))
                            {
                                UpgradeTile(obj, thisTile, hitObject, thatTile);
                                hasMoved = true;
                            }
                            else
                            {
                                Vector3 newPosition = hitObject.transform.position;
                                newPosition.x -= spaceBetweenTiles;
                                if (!Mathf.Approximately(obj.transform.position.x, newPosition.x))
                                {
                                    obj.transform.position = newPosition;
                                    hasMoved = true;
                                }
                            }
                        }
                        else if (hitObject.tag == "Border")
                        {
                            Vector3 newPosition = obj.transform.position;
                            newPosition.x = hit.point.x - halfTileWidth - borderOffset;
                            if (!Mathf.Approximately(obj.transform.position.x, newPosition.x))
                            {
                                obj.transform.position = newPosition;
                                hasMoved = true;
                            }
                        }
                    }
                }
            }
        }

        return hasMoved;
    }

    private bool MoveTilesUp()
    {
        bool hasMoved = false;
        for (int y = 1; y < rows; y++)
        {
            for (int x = 0; x < cols; x++)
            {
                GameObject obj = GetObjectAtGridPosition(x, y);

                if (obj == noTile)
                {
                    continue;
                }

                Vector2 raycastOrigin = obj.transform.position;
                raycastOrigin.y += halfTileWidth;
                RaycastHit2D hit = Physics2D.Raycast(raycastOrigin, Vector2.up, Mathf.Infinity);
                if (hit.collider != null)
                {
                    GameObject hitObject = hit.collider.gameObject;
                    if (hitObject != obj)
                    {
                        if (hitObject.tag == "Tile")
                        {
                            Tile thatTile = hitObject.GetComponent<Tile>();
                            Tile thisTile = obj.GetComponent<Tile>();
                            if (CanUpgrade(thisTile, thatTile))
                            {
                                UpgradeTile(obj, thisTile, hitObject, thatTile);
                                hasMoved = true;
                            }
                            else
                            {
                                Vector3 newPosition = hitObject.transform.position;
                                newPosition.y -= spaceBetweenTiles;
                                if (!Mathf.Approximately(obj.transform.position.y, newPosition.y))
                                {
                                    obj.transform.position = newPosition;
                                    hasMoved = true;
                                }
                            }
                        }
                        else if (hitObject.tag == "Border")
                        {
                            Vector3 newPosition = obj.transform.position;
                            newPosition.y = hit.point.y - halfTileWidth - borderOffset;
                            if (!Mathf.Approximately(obj.transform.position.y, newPosition.y))
                            {
                                obj.transform.position = newPosition;
                                hasMoved = true;
                            }
                        }
                    }
                }
            }
        }

        return hasMoved;
    }

    private bool CanUpgrade(Tile thisTile, Tile thatTile)
    {
        return (thisTile.value != maxValue && thisTile.power == thatTile.power && !thisTile.upgradedThisTurn && !thatTile.upgradedThisTurn);
    }

    private void ReadyTilesForUpgrading()
    {
        foreach (var obj in tiles)
        {
            Tile tile = obj.GetComponent<Tile>();
            tile.upgradedThisTurn = false;
        }
    }

    public void Reset()
    {
        gameOverPanel.SetActive(false);
        foreach (var tile in tiles)
        {
            SimplePool.Despawn(tile);
        }

        tiles.Clear();
        points = 0;
        scoreText.text = "0";
        state = State.Loaded;
    }


    private void UpgradeTile(GameObject toDestroy, Tile destroyTile, GameObject toUpgrade, Tile upgradeTile)
    {
        Vector3 toUpgradePosition = toUpgrade.transform.position;

        tiles.Remove(toDestroy);
        tiles.Remove(toUpgrade);

        SimplePool.Despawn(toDestroy);
        SimplePool.Despawn(toUpgrade);

        // create the upgraded tile
        GameObject obj = SimplePool.Spawn(tilePrefabs[upgradeTile.power], toUpgradePosition, transform.rotation);
        Tile tile = obj.GetComponent<Tile>();
        StartCoroutine(Utils.LoadImageByName(tile.value.ToString(), (texture) =>
        {
            //Debug.Log("load:" + tile.value.ToString() + "===" + texture);
            if (texture != null)
            {
                Sprite sprite = Sprite.Create(texture, new Rect(0, 0, 100, 100), new Vector2(0.5f, 0.5f));
                SpriteRenderer sr = obj.GetComponent<SpriteRenderer>();
                sr.sprite = sprite;
            }

        }));
        tiles.Add(obj);
        tile.upgradedThisTurn = true;

        points += upgradeTile.value * 2;
        scoreText.text = points.ToString();

        TileAnimationHandler tileAnim = obj.GetComponent<TileAnimationHandler>();
        tileAnim.AnimateUpgrade();
    }
    #endregion
}
