using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
//using UnityEditor.Experimental.GraphView;
using UnityEngine.AI;
//using StaticOcclusionCullingVisualization = UnityEditor.StaticOcclusionCullingVisualization;
//using StaticOcclusionCulling = UnityEditor.StaticOcclusionCullingVisualization;

public class WaveFunctionCollapse : MonoBehaviour
{
    public GameObject allProtoPrefab;
    public float gridOffset = 1;
    public Vector2 size;
    public Vector3 startPosition;
    public List<Cell> cells;
    public Dictionary<Vector2, Cell> activeCells = new Dictionary<Vector2, Cell>();
    public List<Cell> cellsAffected = new List<Cell>();
    public Weights weights;
    public GameObject borderPrefab;
    public bool checkSpawn = false;
	public bool spawnAtCenter = true;
    public AreaFloorBaker areaFloorBaker;
    public NavMeshSurface[] Surfaces;
    private Transform player;

    public GameObject[] weaponsToSpawn;
    private int randomNumber; // Индекс позиции для спавна игрока и оружия

    void Start()
    {
        InitializeWaveFunction();
        //StartCoroutine(CollapseOverTime());
    }
    private void LoadData()
    {
        //load dictionary here
    }

    private void Update()
    {
        // Если игрок провалился под землю, то он проигрывает
        if (player.position.y <= -20f)
        {
            SpawnPlayerAtPosition(true);
        }
    }

    public void InitializeWaveFunction()
    {
        areaFloorBaker = GetComponent<AreaFloorBaker>();
        ClearAll();
        for (int x = 0, y = 0; x < size.x; x++)
        {
            for (int z = 0; z < size.y; z++)
            {
                Vector3 pos = new Vector3(x* gridOffset + startPosition.x, 0, z * gridOffset + startPosition.z);

                if(this.gameObject.transform.childCount>y)//kinda breaks
                {
                    GameObject block = this.transform.GetChild(y).gameObject;
                    block.SetActive(true);
                    block.transform.position = pos;        
                }
                else
                {
#if UNITY_EDITOR
                    GameObject block = (GameObject)PrefabUtility.InstantiatePrefab(allProtoPrefab as GameObject);
                    PrefabUtility.UnpackPrefabInstance(block, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                    block.transform.SetParent(this.transform);
                    block.transform.position = pos;
#endif
                }
                Cell cell = this.transform.GetChild(y).gameObject.GetComponent<Cell>();
                cell.coords = new Vector2(x,z);
                cells.Add(cell);
                activeCells.Add(cell.coords, cell);
                y++;
            }
        }
        foreach(Cell c in cells)
            FindNeighbours(c);

        foreach(Cell c in cells)
            c.GenerateWeight(weights);

        //collapsed = 0;
        StartCollapse();

        CreateBorder();
        RandomizeBuildings();
        RandomizeCityDetails();
        if (checkSpawn)
            SpawnPlayerAtPositionCheck(cells);
        else
            SpawnPlayerAtPosition();

        player = GameObject.FindGameObjectWithTag("Player").transform;

        SpawnWeaponsAtPosition();

        Cursor.visible = false;

        PauseController.instance.Starting();

        StaticOcclusionCullingVisualization.showOcclusionCulling = false;
        StaticOcclusionCulling.Clear();

        //Surfaces = GetComponentsInChildren<NavMeshSurface>();
        areaFloorBaker.StartCalculateNavMesh(GetComponents<NavMeshSurface>());

        StaticOcclusionCulling.Compute();
        StaticOcclusionCullingVisualization.showOcclusionCulling = true;

        WaveSystem.instance.Starting(this);
    }
    private void SpawnWeaponsAtPosition()
    {
        PlayerSpawn[] spawnPoint = GetComponentsInChildren<PlayerSpawn>();
        List<int> alreadySpawn = new List<int>();
        alreadySpawn.Add(randomNumber);

        int countSpawn = 2;
        while(countSpawn >= 0)
        {
            int newRandomNumber = Random.Range(0, spawnPoint.Length);

            if(!alreadySpawn.Contains(newRandomNumber))
            {
                Instantiate(weaponsToSpawn[countSpawn], spawnPoint[newRandomNumber].transform.position, Quaternion.identity);
                countSpawn--;
                alreadySpawn.Add(newRandomNumber);
            }
        }
    }
    IEnumerator Teleport(PlayerSpawn[] spawnPoint)
    {
        yield return new WaitForSeconds(1f);
        player.position = spawnPoint[randomNumber].transform.position;
        Time.timeScale = 1f;
    }
    private void SpawnPlayerAtPosition(bool respawn = false)
    {
        if(respawn)
        {
            PlayerSpawn[] spawnPoint = GetComponentsInChildren<PlayerSpawn>();
            //Time.timeScale = 0f;
            player.position = new Vector3(spawnPoint[randomNumber].transform.position.x,
                spawnPoint[randomNumber].transform.position.y + 2,
                spawnPoint[randomNumber].transform.position.z);
            Time.timeScale = 0f;
            Time.timeScale = 1f;
            //StartCoroutine(Teleport(spawnPoint));
        }
        else
        {
            PlayerSpawn[] spawnPoint = GetComponentsInChildren<PlayerSpawn>();

            randomNumber = Random.Range(0, spawnPoint.Length);
            if (spawnAtCenter)
            {
                spawnPoint[spawnPoint.Length / 2].Spawn();
            }
            else
            {
                spawnPoint[randomNumber].Spawn();
            }
        }
    }
    private void SpawnPlayerAtPositionCheck(List<Cell> cells)
    {
        PlayerSpawn[] spawnPoint = GetComponentsInChildren<PlayerSpawn>();

        randomNumber = 0;
        while (true)
        {
            randomNumber = Random.Range(0, spawnPoint.Length);

            foreach (Cell cell in cells)
            {
                cell.visited = false; // Сбрасываем флаг посещения для каждой ячейки
            }

            if (checkAreaSpawn(randomNumber, spawnPoint, cells))
            {
                spawnPoint[randomNumber].Spawn();
                break;
            }
        }
    }
    private bool checkAreaSpawn(int randomNumber, PlayerSpawn[] spawnPoint, List<Cell> cells)
    {
        Cell startingCell = cells.Find(cell => cell.playerSpawn.position == spawnPoint[randomNumber].transform.position);
        Debug.Log("First Cell Name: " + startingCell.name);

        int countSpawnCells = TraverseNeighbors(startingCell);

        if (countSpawnCells > cells.Count / 2)
        {
            Debug.Log("countSpawnCells = " + countSpawnCells + ", cells.Count / 2 = " + cells.Count / 2);
            return true;
        }
        else
        {
            Debug.Log("countSpawnCells = " + countSpawnCells + ", cells.Count / 2 = " + cells.Count / 2);
            return false;
        }
    }
    public int TraverseNeighbors(Cell cell)
    {
        // Проверяем, была ли уже посещена данная ячейка
        //if (cell == null || cell.visited)
            //return 0;

        // Помечаем ячейку как посещенную
        cell.visited = true;

        // Обрабатываем текущую ячейку...

        // Рекурсивно обходим всех соседей
        // Получить валдные цифры для правой стороны текущей ячейки
        int[] tempPosX = new int[3] {
                            cell.currentSinglePrototype._0_identitySides[2],
                            cell.currentSinglePrototype._1_identitySides[2],
                            cell.currentSinglePrototype._2_identitySides[2]
                        };
        // Получить валдные цифры для верхней стороны текущей ячейки
        int[] tempPosZ = new int[3] {
                            cell.currentSinglePrototype._0_identitySides[0],
                            cell.currentSinglePrototype._0_identitySides[1],
                            cell.currentSinglePrototype._0_identitySides[2]
                        };
        // Получить валдные цифры для левой стороны текущей ячейки
        int[] tempNegX = new int[3] {
                            cell.currentSinglePrototype._0_identitySides[0],
                            cell.currentSinglePrototype._1_identitySides[0],
                            cell.currentSinglePrototype._2_identitySides[0]
                        };
        // Получить валдные цифры для нижней стороны текущей ячейки
        int[] tempNegZ = new int[3] {
                            cell.currentSinglePrototype._2_identitySides[0],
                            cell.currentSinglePrototype._2_identitySides[1],
                            cell.currentSinglePrototype._2_identitySides[2]
                        };

        // Гарантируется, что соседние ячейки правильно сопоставлены друг с другом
        if (cell.posXneighbour != null && !cell.posXneighbour.visited)
        {
            // Если текущая ячейка совпадает с ячейкой справа цифрами, КОТОРЫЕ НЕ НУЛИ (все 3 цифры)
            // Иначе нет смысла идти в соседа, который является частью другой части города
            if(!(cell.posXneighbour.currentSinglePrototype._0_identitySides[0] == 0 &&
                cell.posXneighbour.currentSinglePrototype._1_identitySides[0] == 0 &&
                cell.posXneighbour.currentSinglePrototype._2_identitySides[0] == 0 &&
                tempPosX[0] == 0 &&
                tempPosX[1] == 0 &&
                tempPosX[2] == 0))
            {
                Debug.Log("Iterate Cell Name: " + cell.name);
                //countSpawnCells++; // Засчитываем, как дорогу, на которой может появиться игрок
                return 1 + TraverseNeighbors(cell.posXneighbour);
            }
        }
            
        if (cell.negXneighbour != null && !cell.negXneighbour.visited)
        {
            // Если текущая ячейка совпадает с ячейкой слева цифрами, КОТОРЫЕ НЕ НУЛИ (все 3 цифры)
            if (!(cell.negXneighbour.currentSinglePrototype._0_identitySides[2] == 0 &&
                cell.negXneighbour.currentSinglePrototype._1_identitySides[2] == 0 &&
                cell.negXneighbour.currentSinglePrototype._2_identitySides[2] == 0 &&
                tempNegX[0] == 0 &&
                tempNegX[1] == 0 &&
                tempNegX[2] == 0))
            {
                Debug.Log("Iterate Cell Name: " + cell.name);
                //countSpawnCells++; // Засчитываем, как дорогу, на которой может появиться игрок
                return 1 + TraverseNeighbors(cell.negXneighbour);
            }
        }
            
        if (cell.posZneighbour != null && !cell.posZneighbour.visited)
        {
            // Если текущая ячейка совпадает с ячейкой сверху цифрами, КОТОРЫЕ НЕ НУЛИ (все 3 цифры)
            if (!(cell.posZneighbour.currentSinglePrototype._2_identitySides[0] == 0 &&
                cell.posZneighbour.currentSinglePrototype._2_identitySides[1] == 0 &&
                cell.posZneighbour.currentSinglePrototype._2_identitySides[2] == 0 &&
                tempPosZ[0] == 0 &&
                tempPosZ[1] == 0 &&
                tempPosZ[2] == 0))
            {
                Debug.Log("Iterate Cell Name: " + cell.name);
                //countSpawnCells++; // Засчитываем, как дорогу, на которой может появиться игрок
                return 1 + TraverseNeighbors(cell.posZneighbour);
            }
        }

        if (cell.negZneighbour != null && !cell.negZneighbour.visited)
        {
            // Если текущая ячейка совпадает с ячейкой снизу цифрами, КОТОРЫЕ НЕ НУЛИ (все 3 цифры)
            if (!(cell.negZneighbour.currentSinglePrototype._0_identitySides[0] == 0 &&
                cell.negZneighbour.currentSinglePrototype._0_identitySides[1] == 0 &&
                cell.negZneighbour.currentSinglePrototype._0_identitySides[2] == 0 &&
                tempNegZ[0] == 0 &&
                tempNegZ[1] == 0 &&
                tempNegZ[2] == 0))
            {
                Debug.Log("Iterate Cell Name: " + cell.name);
                //countSpawnCells++; // Засчитываем, как дорогу, на которой может появиться игрок
                return 1 + TraverseNeighbors(cell.negZneighbour);
            }
        }

        return 0;
    }
    private void CreateBorder()
    {
        for ( int x = 0; x < size.x; x++ )
        {
            DoInstantiate(borderPrefab, new Vector3(x* gridOffset + startPosition.x,0,-1* gridOffset + startPosition.z), Quaternion.identity, this.transform);
            DoInstantiate(borderPrefab, new Vector3(x* gridOffset + startPosition.x,0,size.y* gridOffset + startPosition.z), Quaternion.Euler(0, 180, 0),this.transform);
        }
        
        for ( int z = 0; z < size.y; z++ )
        {
            DoInstantiate(borderPrefab, new Vector3(-1* gridOffset,0,z* gridOffset + startPosition.z), Quaternion.Euler(0, 90, 0), this.transform);
            DoInstantiate(borderPrefab, new Vector3(size.x* gridOffset + startPosition.x,0,z* gridOffset + startPosition.z), Quaternion.Euler(0, -90, 0), this.transform);
        }
    }
    [ContextMenu("Randomize City Details")]
    public void RandomizeCityDetails()
    {
        CityDetailsRandomizer[] details = GetComponentsInChildren<CityDetailsRandomizer>();
        foreach (CityDetailsRandomizer detail in details)
            detail.RandomizeCityDetails();
    }
    [ContextMenu("Randomize Buildings")]
    public void RandomizeBuildings()
    {
        BuildingRandomizer[] buildings = GetComponentsInChildren<BuildingRandomizer>();
        foreach (BuildingRandomizer b in buildings)
            b.RandomizeBuilding();
    }
    private void DoInstantiate(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent) {
         Transform temp = ((GameObject)Instantiate(prefab,position,rotation)).transform;
         temp.parent = parent;
     }
    private void FindNeighbours(Cell c)
    {
        c.posZneighbour = GetCell(c.coords.x,c.coords.y+1);
        c.negZneighbour = GetCell(c.coords.x,c.coords.y-1);
        c.posXneighbour = GetCell(c.coords.x+1,c.coords.y);
        c.negXneighbour = GetCell(c.coords.x-1,c.coords.y);
    }
    private Cell GetCell(float x, float z)
    {
        Cell cell = null;
        if(activeCells.TryGetValue(new Vector2(x,z), out cell))
            return cell;
        else
            return null;
    }
    int collapsed;
    public void StartCollapse()
    {
        collapsed = 0;
        while(!isCollapsed())
            Iterate();
    }
    public IEnumerator CollapseOverTime()
    {
        while(!isCollapsed())
        {
            Iterate();
            yield return new WaitForSeconds(0.5f);
        }
    }
    private bool isCollapsed()
    {
        foreach(Cell c in cells)
            if(c.possiblePrototypes.Count>1)
                return false;

        return true;
    }
    private void Iterate()
    {
        Cell cell = GetCellWithLowestEntropy();
        CollapseAt(cell);
        Propagate(cell);
    }
    private Cell GetCellWithLowestEntropy()
    {
        List<Cell> cellWithLowestEntropy = new List<Cell>();
        int x = 100000;

        foreach(Cell c in cells)
        {
            if(!c.isCollapsed)
            {
                if(c.possiblePrototypes.Count==x)
                {
                    cellWithLowestEntropy.Add(c);
                }
                else if(c.possiblePrototypes.Count<x)
                {
                    cellWithLowestEntropy.Clear();
                    cellWithLowestEntropy.Add(c);
                    x = c.possiblePrototypes.Count;
                }
            }
        }
        return cellWithLowestEntropy[Random.Range(0, cellWithLowestEntropy.Count)];
    }
    private void CollapseAt(Cell cell)
    {
        int selectedPrototype = SelectPrototype(cell.prototypeWeights);
        Prototype finalPrototype = cell.possiblePrototypes[selectedPrototype];
        finalPrototype.prefab = cell.possiblePrototypes[selectedPrototype].prefab;
        cell.possiblePrototypes.Clear();
        cell.possiblePrototypes.Add(finalPrototype);
        GameObject finalPrefab = Instantiate(finalPrototype.prefab, cell.transform, true);
        finalPrefab.transform.Rotate(new Vector3(0f, finalPrototype.meshRotation*90, 0f), Space.Self);
        finalPrefab.transform.localPosition = Vector3.zero;
        cell.name = cell.coords.ToString()+"_"+ collapsed.ToString();
        /* My */
        cell.currentSinglePrototype = finalPrototype;
        cell.playerSpawn = finalPrefab.GetComponentInChildren<PlayerSpawn>().transform;
        /* My */
        collapsed++;
        cell.isCollapsed = true;
    }
    private int SelectPrototype(List<int> prototypeWeights)
    {
        int total = 0;
        foreach(int weight in prototypeWeights)
            total+=weight;

        total = Random.Range(0, total);

        foreach(int weight in prototypeWeights)
        {
            for (int i = 0; i < prototypeWeights.Count; i++)
            {
                if(total<=prototypeWeights[i])
                {
                    return i;
                }
                else
                    total-=weight;
            }
        }
        return 0;
    }
    private void Propagate(Cell cell)
    {
        cellsAffected.Add(cell);
        int y = 0;
        while(cellsAffected.Count > 0)
        {
            Cell currentCell = cellsAffected[0];
            cellsAffected.Remove(currentCell);

            // Получить соседа справа
            Cell otherCell = currentCell.posXneighbour;
            // Если есть сосед справа
            if(otherCell!=null)
            {
                //Get sockets that we have available on our Right
                //List<WFC_Socket> possibleConnections = GetPossibleSocketsPosX(currentCell.possiblePrototypes);

                // Получить валдные цифры для правой стороны текущей ячейки
                List<int[]> validRightIdentity = GetPossiblePosXIdentity(currentCell.possiblePrototypes);

                bool constrained = false;
                for (int i = 0; i < otherCell.possiblePrototypes.Count; i++)
                {
                    int[] temp = new int[3] { 
                        otherCell.possiblePrototypes[i]._0_identitySides[0],
                        otherCell.possiblePrototypes[i]._1_identitySides[0],
                        otherCell.possiblePrototypes[i]._2_identitySides[0]
                    };
                    // Если сосед не содержит валидных индексов для присоединения справа
                    bool isContained = true;
                    foreach (int[] currSide in validRightIdentity)
                    {
                        if (currSide[0] == temp[0] &&
                            currSide[1] == temp[1] &&
                            currSide[2] == temp[2])
                        {
                            isContained = false;
                            break;
                        }
                    }

                    if(isContained)
                    {
                        // Значит, это не валидный сосед, удаляем его
                        otherCell.possiblePrototypes.RemoveAt(i);
                        otherCell.prototypeWeights.RemoveAt(i);
                        i -= 1;
                        constrained = true;
                    }
                }

                if(constrained)
                    cellsAffected.Add(otherCell);
            }

            // Получить соседа сверху
            otherCell = currentCell.posZneighbour;
            // Если есть сосед сверху
            if (otherCell != null)
            {
                //Get sockets that we have available on our Right
                //List<WFC_Socket> possibleConnections = GetPossibleSocketsPosX(currentCell.possiblePrototypes);

                // Получить валдные цифры для верхней стороны текущей ячейки
                List<int[]> validTopIdentity = GetPossiblePosZIdentity(currentCell.possiblePrototypes);
                bool hasBeenConstrained = false;

                for (int i = 0; i < otherCell.possiblePrototypes.Count; i++)
                {
                    int[] temp = new int[3] {
                        otherCell.possiblePrototypes[i]._2_identitySides[0],
                        otherCell.possiblePrototypes[i]._2_identitySides[1],
                        otherCell.possiblePrototypes[i]._2_identitySides[2]
                    };
                    // Если сосед не содержит валидных индексов для присоединения сверху
                    bool isContained = true;
                    foreach (int[] currSide in validTopIdentity)
                    {
                        if (currSide[0] == temp[0] &&
                            currSide[1] == temp[1] &&
                            currSide[2] == temp[2])
                        {
                            isContained = false;
                            break;
                        }
                    }

                    if (isContained)
                    {
                        // Значит, это не валидный сосед, удаляем его
                        otherCell.possiblePrototypes.RemoveAt(i);
                        otherCell.prototypeWeights.RemoveAt(i);
                        i -= 1;
                        hasBeenConstrained = true;
                    }
                }

                if (hasBeenConstrained)
                    cellsAffected.Add(otherCell);
            }

            // Получить соседа слева
            otherCell = currentCell.negXneighbour;
            // Если есть сосед слева
            if (otherCell != null)
            {
                //Get sockets that we have available on our Right
                //List<WFC_Socket> possibleConnections = GetPossibleSocketsPosX(currentCell.possiblePrototypes);

                // Получить валдные цифры для левой стороны текущей ячейки
                List<int[]> validLeftIdentity = GetPossibleNegXIdentity(currentCell.possiblePrototypes);
                bool hasBeenConstrained = false;

                for (int i = 0; i < otherCell.possiblePrototypes.Count; i++)
                {
                    int[] temp = new int[3] {
                        otherCell.possiblePrototypes[i]._0_identitySides[2],
                        otherCell.possiblePrototypes[i]._1_identitySides[2],
                        otherCell.possiblePrototypes[i]._2_identitySides[2]
                    };
                    // Если сосед не содержит валидных индексов для присоединения слева
                    bool isContained = true;
                    foreach (int[] currSide in validLeftIdentity)
                    {
                        if (currSide[0] == temp[0] &&
                            currSide[1] == temp[1] &&
                            currSide[2] == temp[2])
                        {
                            isContained = false;
                            break;
                        }
                    }

                    if (isContained)
                    {
                        // Значит, это не валидный сосед, удаляем его
                        otherCell.possiblePrototypes.RemoveAt(i);
                        otherCell.prototypeWeights.RemoveAt(i);
                        i -= 1;
                        hasBeenConstrained = true;
                    }
                }

                if (hasBeenConstrained)
                    cellsAffected.Add(otherCell);
            }

            // Получить соседа снизу
            otherCell = currentCell.negZneighbour;
            // Если есть сосед снизу
            if (otherCell != null)
            {
                //Get sockets that we have available on our Right
                //List<WFC_Socket> possibleConnections = GetPossibleSocketsPosX(currentCell.possiblePrototypes);

                // Получить валдные цифры для верхней стороны текущей ячейки
                List<int[]> validBottomIdentity = GetPossibleNegZIdentity(currentCell.possiblePrototypes);
                bool hasBeenConstrained = false;

                for (int i = 0; i < otherCell.possiblePrototypes.Count; i++)
                {
                    int[] temp = new int[3] {
                        otherCell.possiblePrototypes[i]._0_identitySides[0],
                        otherCell.possiblePrototypes[i]._0_identitySides[1],
                        otherCell.possiblePrototypes[i]._0_identitySides[2]
                    };
                    // Если сосед не содержит валидных индексов для присоединения сверху
                    bool isContained = true;
                    foreach (int[] currSide in validBottomIdentity)
                    {
                        if (currSide[0] == temp[0] &&
                            currSide[1] == temp[1] &&
                            currSide[2] == temp[2])
                        {
                            isContained = false;
                            break;
                        }
                    }

                    if (isContained)
                    {
                        // Значит, это не валидный сосед, удаляем его
                        otherCell.possiblePrototypes.RemoveAt(i);
                        otherCell.prototypeWeights.RemoveAt(i);
                        i -= 1;
                        hasBeenConstrained = true;
                    }
                }

                if (hasBeenConstrained)
                    cellsAffected.Add(otherCell);
            }


            y++;
        }
    }
    private List<WFC_Socket> GetPossibleSocketsNegX(List<Prototype> prototypesAvailable)
    {
        List<WFC_Socket> socketsAccepted = new List<WFC_Socket>();
        foreach (Prototype proto in prototypesAvailable)
        {
            if(!socketsAccepted.Contains(proto.negX))
                socketsAccepted.Add(proto.negX);
        }
        return socketsAccepted;
    }
    private List<WFC_Socket> GetPossibleSocketsNegZ(List<Prototype> prototypesAvailable)
    {
        List<WFC_Socket> socketsAccepted = new List<WFC_Socket>();
        foreach (Prototype proto in prototypesAvailable)
        {
            if(!socketsAccepted.Contains(proto.negZ))
                socketsAccepted.Add(proto.negZ);
        }
        return socketsAccepted;
    }
    private List<WFC_Socket> GetPossibleSocketsPosZ(List<Prototype> prototypesAvailable)
    {
        List<WFC_Socket> socketsAccepted = new List<WFC_Socket>();
        foreach (Prototype proto in prototypesAvailable)
        {
            if(!socketsAccepted.Contains(proto.posZ))
                socketsAccepted.Add(proto.posZ);
        }
        return socketsAccepted;
    }
    private List<WFC_Socket> GetPossibleSocketsPosX(List<Prototype> prototypesAvailable)
    {
        List<WFC_Socket> socketsAccepted = new List<WFC_Socket>();
        foreach (Prototype proto in prototypesAvailable)
        {
            if(!socketsAccepted.Contains(proto.posX))
            {
                socketsAccepted.Add(proto.posX);
            }
        }
        return socketsAccepted;
    }

    // Список всех возможных правых сторон для возможных объектов текущей ячейки
    private List<int[]> GetPossiblePosXIdentity(List<Prototype> prototypesAvailable)
    {
        List<int[]> identityAccepted = new List<int[]>();

        foreach (Prototype proto in prototypesAvailable)
        {
            int[] temp = new int[3] { 
                proto._0_identitySides[2], 
                proto._1_identitySides[2], 
                proto._2_identitySides[2]
            };
            // Если список не содержит 3 валидных цифры для правой стороны
            if (!identityAccepted.Contains(temp))
            {
                // Debug.Log($"Adding {proto.posX}, to the list of accepted sockets for {proto.name}");
                // Добавить
                identityAccepted.Add(temp);
            }
        }

        return identityAccepted;
    }
    // Список всех возможных левых сторон для возможных объектов текущей ячейки
    private List<int[]> GetPossibleNegXIdentity(List<Prototype> prototypesAvailable)
    {
        List<int[]> identityAccepted = new List<int[]>();

        foreach (Prototype proto in prototypesAvailable)
        {
            int[] temp = new int[3] { 
                proto._0_identitySides[0], 
                proto._1_identitySides[0], 
                proto._2_identitySides[0]
            };
            // Если список не содержит 3 валидных цифры для правой стороны
            if (!identityAccepted.Contains(temp))
            {
                // Добавить
                identityAccepted.Add(temp);
            }
        }

        return identityAccepted;
    }
    // Список всех возможных верхних сторон для возможных объектов текущей ячейки
    private List<int[]> GetPossiblePosZIdentity(List<Prototype> prototypesAvailable)
    {
        List<int[]> identityAccepted = new List<int[]>();

        foreach (Prototype proto in prototypesAvailable)
        {
            int[] temp = new int[3] {
                proto._0_identitySides[0],
                proto._0_identitySides[1],
                proto._0_identitySides[2]
            };
            // Если список не содержит 3 валидных цифры для правой стороны
            if (!identityAccepted.Contains(temp))
            {
                // Добавить
                identityAccepted.Add(temp);
            }
        }

        return identityAccepted;
    }
    // Список всех возможных нижних сторон для возможных объектов текущей ячейки
    private List<int[]> GetPossibleNegZIdentity(List<Prototype> prototypesAvailable)
    {
        List<int[]> identityAccepted = new List<int[]>();

        foreach (Prototype proto in prototypesAvailable)
        {
            int[] temp = new int[3] {
                proto._2_identitySides[0],
                proto._2_identitySides[1],
                proto._2_identitySides[2]
            };
            // Если список не содержит 3 валидных цифры для правой стороны
            if (!identityAccepted.Contains(temp))
            {
                // Добавить
                identityAccepted.Add(temp);
            }
        }

        return identityAccepted;
    }

    private bool Constrain(Cell otherCell, WFC_Socket socketItMustPairWith)
    {
        bool hasBeenConstrained = false;
        
        // Проверка всех соседей
        for (int i = 0; i < otherCell.possiblePrototypes.Count; i++)
        {
            // if(otherCell.possiblePrototypes[i])
            // List<WFC_Socket> socketsAccepted = new List<WFC_Socket>();
            // socketsAccepted.AddRange(GetPossibleSockets(currentCell.possiblePrototypes));
            // Debug.Log($"Sockets accepted {socketsAccepted.Count}");
            // if(HasAConnector(currentCell.possiblePrototypes[0].negX, otherCell.possiblePrototypes[i].posX))
            // {
            //     otherCell.possiblePrototypes.RemoveAt(i);
            //     i-=1;
            //     hasBeenConstrained = true;
            // }
            // else if(HasAConnector(socketsAccepted, otherCell.possiblePrototypes[i].posZ))
            // {
            //     otherCell.possiblePrototypes.RemoveAt(i);
            //     i-=1;
            //     hasBeenConstrained = true;
            // }
            // else if(HasAConnector(socketsAccepted, otherCell.possiblePrototypes[i].negX))
            // {
            //     otherCell.possiblePrototypes.RemoveAt(i);
            //     i-=1;
            //     hasBeenConstrained = true;
            // }
            // else if(HasAConnector(socketsAccepted, otherCell.possiblePrototypes[i].negZ))
            // {
            //     otherCell.possiblePrototypes.RemoveAt(i);
            //     i-=1;
            //     hasBeenConstrained = true;
            // }
        }
        return hasBeenConstrained;
    }
    private bool HasAConnector(List<WFC_Socket> socketsAccepted, WFC_Socket thisSocket)
    {
        foreach (WFC_Socket s in socketsAccepted)
        {
            if(s == thisSocket)
                return true;
        }
        return false;
    }
    private List<WFC_Socket> GetPossibleSockets(List<Prototype> possibleNeighbors)
    {
        List<WFC_Socket> socketsAccepted = new List<WFC_Socket>();
        foreach (Prototype proto in possibleNeighbors)
        {
            if(!socketsAccepted.Contains(proto.posX))
                socketsAccepted.Add(proto.posX);
            if(!socketsAccepted.Contains(proto.negX))
                socketsAccepted.Add(proto.negX);
            if(!socketsAccepted.Contains(proto.posZ))
                socketsAccepted.Add(proto.posZ);
            if(!socketsAccepted.Contains(proto.negZ))
                socketsAccepted.Add(proto.negZ);
        }
        return socketsAccepted;
    }
    public void ClearAll()
    {
#if UNITY_EDITOR
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if(player != null)
            DestroyImmediate(player.transform.parent.gameObject);
        cells.Clear();
        activeCells.Clear();
        for(int i = this.transform.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(this.transform.GetChild(i).gameObject);
        }
#else
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if(player != null)
            Destroy(player.transform.parent.gameObject);
        cells.Clear();
        activeCells.Clear();
        for(int i = this.transform.childCount - 1; i >= 0; i--)
        {
            Destroy(this.transform.GetChild(i).gameObject);
        }
#endif
    }
}
