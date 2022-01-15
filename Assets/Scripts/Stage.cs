using UnityEngine;

public class Stage : MonoBehaviour {
    [Header("Editor Objects")]
    public GameObject cubePrefab;
    public Transform backgroundNode;
    public Transform boardNode;
    public Transform tetracubeNode;

    [Header("Game Settings")]
    [Range(4, 40)]
    public int boardWidth = 5;
    [Range(5, 20)]
    public int boardHeight = 10;
    public float fallCycle = 2.0f;
    private bool fall = false;
    public bool getFall() {
        return this.fall;
    }

    public Cube CreateCube(Transform parent, Vector3 position, Color color, int order = 1) {
        var go = Instantiate(cubePrefab);
        go.transform.parent = parent;
        go.transform.localPosition = position;

        var cube = go.GetComponent<Cube>();
        cube.color = color;
        cube.sortingOrder = order;

        return cube;
    }

    private int halfWidth;
    private int halfHeight;
    private float nextFallTime;
    

    private void Start() {
        halfWidth = Mathf.RoundToInt(boardWidth * 0.5f);
        halfHeight = Mathf.RoundToInt(boardHeight * 0.5f);
        CreateTetracube();
    }

    private void Update() {
        Vector3 moveDir = Vector3.zero;
        Quaternion rotDir = Quaternion.identity;
        bool isRotate = false;
        // Move
        if (Input.GetKeyDown(KeyCode.LeftArrow)) {
            moveDir.x = -1;
        }
        if (Input.GetKeyDown(KeyCode.RightArrow)) {
            moveDir.x = 1;
        }

        if (Input.GetKeyDown(KeyCode.UpArrow)) {
            moveDir.z = 1;
        }
        if (Input.GetKeyDown(KeyCode.DownArrow)) {
            moveDir.z = -1;
        }

        // Rotate
        if (Input.GetKeyDown(KeyCode.W)) {
            tetracubeNode.Rotate(new Vector3(0, 90, 0), Space.World);
            isRotate = true;
        }
        if (Input.GetKeyDown(KeyCode.A)) {
            tetracubeNode.Rotate(new Vector3(90, 0, 0), Space.World);
            isRotate = true;
        }
        if (Input.GetKeyDown(KeyCode.D)) {
            tetracubeNode.Rotate(new Vector3(0, 0, 90), Space.World);
            isRotate = true;
        }

        if (Time.time > nextFallTime) {
            nextFallTime = Time.time + fallCycle;
            moveDir = Vector3.down;
            isRotate = false;
            fall = true;
        } else {
            fall = false;
        }
        
        if (moveDir != Vector3.zero) {
            MoveTetracube(moveDir);
        }
        if (isRotate) {
            tetracubeNode.transform.rotation *= rotDir;
        }
    }

    private bool MoveTetracube(Vector3 moveDir) {
        Vector3 oldPos = tetracubeNode.transform.position;
        Quaternion oldRot = tetracubeNode.transform.rotation;

        tetracubeNode.transform.position += moveDir;

        if (!CanMoveTo(tetracubeNode))
        {
            tetracubeNode.transform.position = oldPos;
            tetracubeNode.transform.rotation = oldRot;

            if ((int)moveDir.y == -1 && (int)moveDir.x == 0 && (int)moveDir.z == 0)
            {
                AddToBoard(tetracubeNode);
                CheckBoardColumn();
                CreateTetracube();
            }

            return false;
        }
        return true;
    }

    // private bool RotateTetracube(Quaternion rotDir) {
    //     tetracubeNode.transform.rotation *= rotDir;
    //     return true;
    // }
    
    public void RotateTetracube(int rotDir) {
        switch (rotDir) {
            case 1:
                tetracubeNode.Rotate(new Vector3(0, 90, 0), Space.World);
                break;
            case 2:
                tetracubeNode.Rotate(new Vector3(90, 0, 0), Space.World);
                break;
            case 3:
                tetracubeNode.Rotate(new Vector3(0, 0, 90), Space.World);
                break;
        }
    }

    bool CanMoveTo(Transform root)
    {
        for (int i = 0; i < root.childCount; ++i)
        {
            var node = root.GetChild(i);
            int x = Mathf.RoundToInt(node.transform.position.x);
            int y = Mathf.RoundToInt(node.transform.position.y - 0.5f);
            int z = Mathf.RoundToInt(node.transform.position.z);

            // if (x < 0 || x > boardWidth - 1)
            //     return false;

            if (y < 0)
                return false;

            var column = boardNode.Find(y.ToString());

            if (column != null && column.Find(x.ToString() + ", " + z.ToString()) != null) {
                return false;
            }
        }

        return true;
    }

    void AddToBoard(Transform root)
    {
        while (root.childCount > 0)
        {
            var node = root.GetChild(0);

            int x = Mathf.RoundToInt(node.transform.position.x);
            int y = Mathf.RoundToInt(node.transform.position.y - 0.5f);
            int z = Mathf.RoundToInt(node.transform.position.z);
            if ((Mathf.Abs(x) == 2 || Mathf.Abs(z) == 2) &&
                (Mathf.Abs(x) <= 2 && Mathf.Abs(z) <= 2)) {
                node.parent = boardNode.Find(y.ToString());
                node.name = x.ToString() + ", " + z.ToString();
            } else {
                node.parent = boardNode.Find("trash");
            }
        }
    }

    void CheckBoardColumn() {
        bool isCleared = false;
        int fullBlockNum = 4 * boardWidth;
        foreach (Transform column in boardNode) {
            if (column.name == "trash") {
                continue;
            }
            if (column.transform.childCount == fullBlockNum) {
                Debug.Log("Destroy");
                foreach (Transform tile in column) {
                    Destroy(tile.gameObject);
                }

                column.DetachChildren();
                isCleared = true;
            }
        }
        if (isCleared) {
            for (int i = 0; i < boardHeight; ++i) {
                var column = boardNode.Find(i.ToString());

                // 이미 비어 있는 행은 무시
                if (column.transform.childCount == 0) {
                    continue;
                }

                int emptyCol = 0;
                int j = i - 1;
                while (j >= 0) {
                    if (boardNode.Find(j.ToString()).childCount == 0) {
                        emptyCol++;
                    }
                    j--;
                }

                if (emptyCol > 0) {
                    var targetColumn = boardNode.Find((i - emptyCol).ToString());

                    while (column.childCount > 0) {
                        Transform tile = column.GetChild(0);
                        tile.parent = targetColumn;
                        tile.transform.position += new Vector3(0, -emptyCol, 0);
                    }
                    column.DetachChildren();
                }
            }
        }
    }

private void CreateTetracube() {
        int index = Random.Range(0, 8);
        // Debug.Log(index);
        Color32 color = Color.white;

        tetracubeNode.rotation = Quaternion.identity;
        tetracubeNode.position = new Vector3(0f, boardHeight + 0.5f, 0f);
        // Debug.Log("switch");

        switch (index) {
            // Cube(1) : �ϴû�
            case 0:
                color = new Color32(115, 251, 253, 255);
                CreateCube(tetracubeNode, new Vector3(0.0f, 0.0f, 0.0f), color);
                CreateCube(tetracubeNode, new Vector3(0.0f, 0.0f, 1.0f), color);
                CreateCube(tetracubeNode, new Vector3(1.0f, 0.0f, 0.0f), color);
                CreateCube(tetracubeNode, new Vector3(0.0f, 1.0f, 0.0f), color);
                // Debug.Log("1");
                break;

            // Cube(2) : �Ķ���
            case 1:
                color = new Color32(0, 33, 245, 255);
                CreateCube(tetracubeNode, new Vector3(0.0f, 0.0f, 0.0f), color);
                CreateCube(tetracubeNode, new Vector3(0.0f, 0.0f, 1.0f), color);
                CreateCube(tetracubeNode, new Vector3(1.0f, 0.0f, 0.0f), color);
                CreateCube(tetracubeNode, new Vector3(1.0f, 1.0f, 0.0f), color);
                // Debug.Log("1");
                break;

            // Cube(3) : �ֻ�
            case 2:
                color = new Color32(243, 168, 59, 255);
                CreateCube(tetracubeNode, new Vector3(0.0f, 0.0f, 0.0f), color);
                CreateCube(tetracubeNode, new Vector3(0.0f, 0.0f, 1.0f), color);
                CreateCube(tetracubeNode, new Vector3(1.0f, 0.0f, 0.0f), color);
                CreateCube(tetracubeNode, new Vector3(1.0f, 0.0f, 1.0f), color);
                // Debug.Log("1");
                break;

            // Cube(4) : �����
            case 3:
                color = new Color32(255, 253, 84, 255);
                CreateCube(tetracubeNode, new Vector3(0.0f, 0.0f, 0.0f), color);
                CreateCube(tetracubeNode, new Vector3(0.0f, 0.0f, 1.0f), color);
                CreateCube(tetracubeNode, new Vector3(1.0f, 0.0f, 0.0f), color);
                CreateCube(tetracubeNode, new Vector3(0.0f, 0.0f, 2.0f), color);
                // Debug.Log("1");
                break;

            // Cube 5 : ������
            case 4:
                color = new Color32(0, 0, 0, 255);
                CreateCube(tetracubeNode, new Vector3(0f, 0f, 0f), color);
                CreateCube(tetracubeNode, new Vector3(0f, 0f, 1f), color);
                CreateCube(tetracubeNode, new Vector3(1f, 0f, 0f), color);
                CreateCube(tetracubeNode, new Vector3(0f, 0f, -1f), color);
                // Debug.Log("1");
                break;

            // Cube 6 : �Ͼ��
            case 5:
                color = new Color32(255, 255, 255, 255);
                CreateCube(tetracubeNode, new Vector3(0f, 0f, 0f), color);
                CreateCube(tetracubeNode, new Vector3(0f, 0f, 1f), color);
                CreateCube(tetracubeNode, new Vector3(1f, 0f, 0f), color);
                CreateCube(tetracubeNode, new Vector3(-1f, 0f, 1f), color);
                // Debug.Log("1");
                break;

            // Cube 7 : ���ֻ�
            case 6:
                color = new Color32(155, 47, 246, 255);
                CreateCube(tetracubeNode, new Vector3(0f, 0f, 0f), color);
                CreateCube(tetracubeNode, new Vector3(0f, 0f, 1f), color);
                CreateCube(tetracubeNode, new Vector3(0f, 0f, -1f), color);
                CreateCube(tetracubeNode, new Vector3(0f, 0f, 2f), color);
                // Debug.Log("1");
                break;

            // Cube 8 : ������
            case 7:
                color = new Color32(235, 51, 35, 255);
                CreateCube(tetracubeNode, new Vector3(0f, 0f, 0f), color);
                CreateCube(tetracubeNode, new Vector3(0f, 0f, 1f), color);
                CreateCube(tetracubeNode, new Vector3(1f, 0f, 0f), color);
                CreateCube(tetracubeNode, new Vector3(0f, 1f, 1f), color);
                // Debug.Log("1");
                break;
        }
    }
}