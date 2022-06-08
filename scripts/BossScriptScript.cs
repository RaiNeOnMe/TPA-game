using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class BossScript : MonoBehaviour
{
    static int MinX, MaxX, MinZ, MaxZ;
    static float GridSize = 1f;
    static float MaxSteep = 1.5f;
    static float EndX, EndZ;
    static float StopAstarRange;
    static float ShootingRange;
    static float ChasingRange;
    public Transform prefab;
    static public Transform target; 
    static public bool BossStart;

    // points for patrol
    public GameObject GlobalObject;

    private Animator anim;
    public GameObject Player;
    float MoveSpeed;
    float RotateSpeed; 
    [SerializeField] private LayerMask ground;
    private bool isGrounded;
    private float gravity = -9.8f;
    private float groundDistance;
    private Vector3 velocity;
    private CharacterController controller;

    // A STAR
    List<Vector2> Path;
    bool IsChasing;
    bool IsShooting;
    float StartShootingTime, StartChasingTime;
    float ShootingDuration, ChasingDuration;
    float LastAStar;
    float AStarCoolDown;
    float PointDistance;
    float speed;

    // HP system
    float hp;
    float maxHp;
    bool life;
    //UIBoss uiBoss;


    void Start()
    {
        anim = GetComponent<Animator>();    
        controller = GetComponent<CharacterController>();
        target = GameObject.Find("char").transform;
        MinX=0;
        MaxX=700;
        MinZ=0;
        MaxZ=300;
        MoveSpeed=6f;
        RotateSpeed = 3f;
        StopAstarRange = 2;
        IsChasing = true;
        IsShooting = false;
        LastAStar = 1;
        AStarCoolDown = 2f;
        PointDistance = 2f;
        Path = new List<Vector2>();
        BossStart = false;

        life = true;
        maxHp = 2000f;
        hp = maxHp;
        //uiBoss = GetComponentInChildren<UIBoss>();


        ChasingDuration = 5f;
        ShootingDuration = 5f;

        StartChasingTime = Time.time;
        IsChasing = true;
    }

    void Update(){
        if(BossStart == false) return;
        GetComponent<Animator>().SetFloat("Shooting", 1f);

        ChaseOrShoot();

        // gravity
        isGrounded = Physics.CheckSphere(transform.position, groundDistance, ground); // checkk apakah colide disekitarnya
        if(isGrounded && velocity.y < 0)
        {
            velocity.y = 2f; // biar ga melayang
        }
        velocity.y += gravity * Time.deltaTime;
        // controller.Move(velocity * Time.deltaTime);
    }

    void ChaseOrShoot(){

        if(IsShooting){
            if(Time.time > StartShootingTime + ShootingDuration){
                IsShooting=false;
                IsChasing=true;
                StartChasingTime = Time.time;
            }
        }else{
            Debug.Log("boss distance " + getDistance(
                    target.position.x,
                    target.position.z,
                    transform.position.x,
                    transform.position.z
                ));
            if(
                Time.time > StartChasingTime + ChasingDuration
                || getDistance(
                    target.position.x,
                    target.position.z,
                    transform.position.x,
                    transform.position.z
                ) < StopAstarRange + 3
            ){

                IsShooting = true;
                IsChasing = false;
                StartShootingTime = Time.time;
            }
        }


        if(IsShooting){
            Shooting();
        }else{
            Chasing();
        }

    }
    public static float getHeuristic(float X, float Z){
        X += (GridSize/2);
        Z += (GridSize/2);
        float GCost = Mathf.Sqrt(Mathf.Pow((X-EndX), 2) + Mathf.Pow((Z-EndZ), 2) * 1f);
        float HCost = Mathf.Sqrt(Mathf.Pow((X-target.position.x), 2) + Mathf.Pow((Z-target.position.z), 2) * 1f);
        return GCost + HCost;
    }
    public class Node{
        public int PrevX, PrevZ;
        public int NextX, NextZ;
        public int X, Z;
        public bool Visited; 
        public float Heuristic;
        public Node(){}
        public Node(int X, int Z){
            this.X = X;
            this.Z = Z;
            this.PrevX = -1; // default value
            this.PrevZ = -1;
            this.NextX = -1;
            this.NextZ = -1;
            this.Visited = false;
            this.Heuristic = getHeuristic(X, Z);
        }
        public void SetPrev(int X, int Z){
            this.PrevX = X;
            this.PrevZ = Z;
        }
        public void SetNext(int X, int Z){
            this.NextX = X;
            this.NextZ = Z;
        }
        public void MarkVisit(){
            this.Visited = true;
        }
    }
    
    public Node[,] Field;

    public Node GetField(int X, int Z){
        if(Field[X,Z]==null){
            Field[X,Z] = new Node(X,Z);
        }
        return Field[X,Z];
    }

    public bool CheckPos(int CurrentX, int CurrentZ, int PrevX, int PrevZ){
        // UnityEngine.Debug.Log("Boss check pos : " + CurrentX + " " + CurrentZ);
        Vector3 Current = new Vector3(CurrentX, 0, CurrentZ);
        Vector3 Prev = new Vector3(PrevX, 0, PrevZ);
        if(!(CurrentX >= MinX && CurrentX <= MaxX && CurrentZ >= MinZ && CurrentZ <= MaxZ))
        {
            Debug.Log("statement 1 true");
        }
        if(!(GetField(CurrentX, CurrentZ).Visited == false))
        {
            Debug.Log("statement 2 true");
        }
        if(!(Mathf.Abs(Terrain.activeTerrain.SampleHeight(Current) - Terrain.activeTerrain.SampleHeight(Prev)) < MaxSteep))
        {
            Debug.Log("statement 3 true");
        }
        if(!(astarMapper.ValidField[CurrentX, CurrentZ]))
        {
            Debug.Log("statement 4 true");
        }
        if(
            CurrentX>=MinX && CurrentX<=MaxX && CurrentZ>=MinZ && CurrentZ<=MaxZ // in of bounds
            && GetField(CurrentX, CurrentZ).Visited==false // not visited
            && Mathf.Abs(Terrain.activeTerrain.SampleHeight(Current) - Terrain.activeTerrain.SampleHeight(Prev)) < MaxSteep // not too steep
            && astarMapper.ValidField[CurrentX, CurrentZ] // not colliding
        ) return true;
        return false;
    }

    public float getDistance(float X1, float Z1, float X2, float Z2){
        return Mathf.Sqrt((Mathf.Pow((X1-X2), 2f) + Mathf.Pow((Z1-Z2), 2f)));
    }

    public List<Vector2> pathfind(int StartX, int StartZ, int X2, int Z2, float StopAstarRange){
        List<Vector2> Result = new List<Vector2>();
        EndX = X2;
        EndZ = Z2;
        Field = new Node[MaxX + 2, MaxZ + 2];
        bool found = false;
        int LastX=-1, LastZ=-1;
        

        // add first grid to the queue
        GetField(StartX, StartZ).MarkVisit();
        List<Node> Queue = new List<Node>();
        Node TempNode = new Node(StartX, StartZ);
        Queue.Add(TempNode);

        //  loop while queue not empty and not found
        while(Queue.Count > 0 && !found){

            // get node with least heuristic
            float LeastHeuristic = 10000000f;
            int LeastIndex = -1;
            for(int i=0; i<Queue.Count; i++){
                if(Queue[i].Heuristic < LeastHeuristic){
                    LeastIndex = i;
                    LeastHeuristic = Queue[i].Heuristic;
                }
            }

            // check the current node
            Node Check = Queue[LeastIndex];
            // UnityEngine.Debug.Log("visiting " + Check.X + " " + Check.Z);
            Queue.RemoveAt(LeastIndex);
            if(getDistance(Check.X, Check.Z, EndX, EndZ) <= StopAstarRange){
                found = true;
                // UnityEngine.Debug.Log("found " + Check.X + " " + Check.Z);
                LastX = Check.X;
                LastZ = Check.Z;
                break;
            }
            Debug.Log(Check.X + " boss visiting " +Check.Z);

            for (int i=-1; i<=1; i++){
                for(int k=-1; k<=1; k++){
                    if(i==0 && k==0) continue;
                    int TempX = Check.X + i;
                    int TempZ = Check.Z + k;
                    if(CheckPos(TempX, TempZ, Check.X, Check.Z)){
                        int CountSide = 0;
                        if(k==-1 || k==1) CountSide++;
                        if(i==-1 || i==1) CountSide++;
                        if(CountSide==2){
                            if(!(CheckPos(TempX-i, TempZ, Check.X, Check.Z) && CheckPos(TempX, TempZ-k, Check.X, Check.Z))) continue;
                        }
                        GetField(TempX, TempZ).MarkVisit();
                        GetField(TempX, TempZ).SetPrev(Check.X, Check.Z);
                        TempNode = new Node(TempX, TempZ);
                        Queue.Add(TempNode);
                    }else{
                        // UnityEngine.Debug.Log("Invalid arouund");
                    }
                }
            }
        }


        if(found){
            Debug.Log("boss found player");
            int CurrentX = LastX;
            int CurrentZ = LastZ;
            int PrevX=-1, PrevZ=-1, NextX, NextZ;
            while(GetField(CurrentX, CurrentZ).PrevX > 0){
                // UnityEngine.Debug.Log("back tracking " + CurrentX + " " + CurrentZ);
                PrevX = GetField(CurrentX, CurrentZ).PrevX;
                PrevZ = GetField(CurrentX, CurrentZ).PrevZ;
                GetField(PrevX, PrevZ).SetNext(CurrentX, CurrentZ);
                CurrentX = PrevX;
                CurrentZ = PrevZ;
            }

            CurrentX = StartX;
            CurrentZ = StartZ;

            while(GetField(CurrentX, CurrentZ).NextX > 0 &&  GetField(CurrentX, CurrentZ).NextZ > 0){
                // Instantiate(prefab, new Vector3(CurrentX, Terrain.activeTerrain.SampleHeight(new Vector3(CurrentX,1f, CurrentZ)), CurrentZ), Quaternion.identity);
                Result.Add(new Vector2(CurrentX, CurrentZ));
                NextX = GetField(CurrentX, CurrentZ).NextX;
                NextZ = GetField(CurrentX, CurrentZ).NextZ;
                CurrentX = NextX;
                CurrentZ = NextZ;
            }
        }else{
            Debug.Log("boss not found");
            // UnityEngine.Debug.Log("Not found");
        }
        return Result;
    }


    void FaceToPlayer(){
        var lookTo = Player.transform.position - transform.position;
        lookTo.y = 0;
        var rotation = Quaternion.LookRotation(lookTo);
        transform.rotation = Quaternion.Slerp(transform.rotation, rotation, Time.deltaTime * RotateSpeed);
    }

    void FaceToPosition(Vector3 Pos){
        var lookTo = Pos - transform.position;
        lookTo.y = 0;
        var rotation = Quaternion.LookRotation(lookTo);
        transform.rotation = Quaternion.Slerp(transform.rotation, rotation, Time.deltaTime * RotateSpeed);
    }

    void Shooting(){
        FaceToPlayer();
        IsShooting = true;
        IsChasing = false;
        GetComponent<Animator>().SetFloat("Shooting", 1f);
        
        BossWeapon weapon = GetComponentInParent<BossWeapon>();
        weapon.ShootToPlayer();
    }
    void Chasing(){
        Debug.Log("boss is chasing");
        IsShooting=false;
        IsChasing=true;
        GetComponent<Animator>().SetFloat("Shooting", 0f);
        if(Time.time > LastAStar + AStarCoolDown){
            // update pathfinder
            Debug.Log("boss updating path");
            LastAStar = Time.time;
            Path = pathfind(
                Mathf.FloorToInt(transform.position.x), 
                Mathf.FloorToInt(transform.position.z), 
                Mathf.FloorToInt(Player.transform.position.x), 
                Mathf.FloorToInt(Player.transform.position.z), 
                StopAstarRange
            );
        }
        Vector2 NextPoint2;
        Vector3 NextPoint3;
        float height;
        bool Repeat = false;
        do{
            if(Path.Count <= 0) return;
            Repeat = false;
            NextPoint2 = Path[0];
            NextPoint3 = new Vector3(NextPoint2.x, 0f, NextPoint2.y);
            height = Terrain.activeTerrain.SampleHeight(NextPoint3);
            NextPoint3.y = height;
            if(getDistance(
                NextPoint3.x,
                NextPoint3.z,
                transform.position.x,
                transform.position.z
            ) < PointDistance){
                Repeat = true;
                Path.RemoveAt(0);
            }
        } while (Repeat);
        Debug.Log("boss visiting "+NextPoint3);
        FaceToPosition(NextPoint3);
        speed = MoveSpeed * Time.deltaTime;
        transform.position =  Vector3.MoveTowards(transform.position, NextPoint3, speed);
        // UnityEngine.Debug.Log("moving boss to" + Path[0].x + " " + NextPoint3.y + " " + Path[0].y);
    }

    public void StartBoss(){
        BossStart = true;
        GetComponent<Animator>().SetFloat("Shooting", 0f);
    }
    
    //public void TakeDamage(float damageReceived){
    //    Debug.Log("take damage of " + damageReceived);
    //    hp -= damageReceived;
    //    if(hp < 0) Die();
    //    GameObject.Find("BossMenu").GetComponent<UIBoss>().SetHealthPercentage(hp, maxHp);
    //}

    //void Die(){
    //    life = false;
    //    GetComponent<Animator>().SetBool("Died", true);
    //    GameStateScript.Won();
    //}

}