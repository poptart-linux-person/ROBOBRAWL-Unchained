using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

public static class RobotArenaBootstrap
{
    static GameObject root;
    static GameObject robotPrefab;
    static readonly Vector3[] spawns = { new(-20,1.3f,-12),new(-10,1.3f,-8),new(0,1.3f,-12),new(10,1.3f,-8),new(20,1.3f,-12),new(-20,1.3f,12),new(-10,1.3f,8),new(0,1.3f,12),new(10,1.3f,8),new(20,1.3f,12),new(-26,1.3f,0),new(26,1.3f,0) };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        if (root != null) return;
        root = new GameObject("ROBOBRAWL Runtime"); BuildLighting(); BuildArena(); BuildHangout();
        var manager=BuildNetworkManager(); BuildPlayerPrefab(manager); BuildRobotPrefab(manager);
        var nm=manager.GetComponent<NetworkManager>(); nm.OnServerStarted += SpawnRobots;
        if(!Application.isBatchMode) nm.StartHost();
    }

    static void BuildLighting(){var light=new GameObject("Arena Sun");var sun=light.AddComponent<Light>();sun.type=LightType.Directional;sun.intensity=1.25f;light.transform.rotation=Quaternion.Euler(48,-28,0);RenderSettings.ambientLight=new Color(.24f,.28f,.32f);}

    static void BuildArena()
    {
        MakeBlock("Grass Arena",new Vector3(0,-.35f,0),new Vector3(64,.7f,48),new Color(.2f,.63f,.21f),"ArenaWall");
        MakeBlock("Concrete Ring",new Vector3(0,.03f,0),new Vector3(48,.14f,32),new Color(.42f,.44f,.46f),"ArenaWall");
        MakeBlock("Inner Turf",new Vector3(0,.12f,0),new Vector3(44,.12f,28),new Color(.25f,.72f,.24f),"ArenaWall");
        MakeWall(new Vector3(-32,5,0),new Vector3(1,10,48),new Color(.8f,.18f,.16f));MakeWall(new Vector3(32,5,0),new Vector3(1,10,48),new Color(.22f,.35f,.8f));MakeWall(new Vector3(0,5,-24),new Vector3(64,10,1),new Color(.8f,.48f,.12f));MakeWall(new Vector3(0,5,24),new Vector3(64,10,1),new Color(.62f,.2f,.72f));
        for(int i=0;i<16;i++){float x=Mathf.Cos(i*.91f)*23,z=Mathf.Sin(i*.91f)*15;MakeBlock("Arena Prop",new Vector3(x,1,z),new Vector3(Random.Range(.7f,2f),Random.Range(1f,2.7f),Random.Range(.7f,2f)),new Color(Random.value*.5f+.3f,Random.value*.5f+.3f,Random.value*.5f+.3f),"ArenaWall",true);}
    }

    static void BuildHangout(){MakeBlock("Hangout Floor",new Vector3(0,-.28f,33),new Vector3(64,.5f,18),new Color(.34f,.22f,.11f),"ArenaWall");for(int i=0;i<10;i++)MakeBlock("Hangout Pillar",new Vector3(-27+i*6,3.5f,33),new Vector3(2,7,2),new Color(.1f+.05f*i,.3f,.5f+.03f*i),"ArenaWall",true);MakeBlock("Hangout Sign",new Vector3(0,6,33),new Vector3(16,2,1),new Color(.95f,.2f,.15f),"ArenaWall",true);}

    static GameObject BuildNetworkManager(){var go=new GameObject("NetworkManager");var nm=go.AddComponent<NetworkManager>();var transport=go.AddComponent<UnityTransport>();nm.NetworkConfig=new NetworkConfig{ProtocolVersion=1,TickRate=50,EnableSceneManagement=false,NetworkTransport=transport};Object.DontDestroyOnLoad(go);return go;}

    static void BuildPlayerPrefab(GameObject managerObject)
    {
        var nm=managerObject.GetComponent<NetworkManager>();var p=GameObject.CreatePrimitive(PrimitiveType.Capsule);p.name="PlayerNetworkPrefab";p.tag="Player";p.transform.localScale=new Vector3(.4f,.8f,.4f);
        Object.DestroyImmediate(p.GetComponent<CapsuleCollider>());p.AddComponent<NetworkObject>();p.AddComponent<CharacterController>();p.AddComponent<NetworkTransform>();p.AddComponent<CombatBody>();p.AddComponent<PlayerCombat>();var locomotion=p.AddComponent<GorillaLocomotion>();
        var head=new GameObject("Head").transform;head.SetParent(p.transform,false);var left=new GameObject("LeftHand").transform;left.SetParent(p.transform,false);var right=new GameObject("RightHand").transform;right.SetParent(p.transform,false);
        var rig=p.AddComponent<VRTrackedRig>();rig.head=head;rig.leftHand=left;rig.rightHand=right;locomotion.head=head;locomotion.leftHand=left;locomotion.rightHand=right;
        var camGo=new GameObject("VR Camera");camGo.transform.SetParent(head,false);camGo.transform.localPosition=Vector3.zero;camGo.tag="MainCamera";camGo.AddComponent<Camera>();camGo.AddComponent<AudioListener>();camGo.AddComponent<LocalOnlyCamera>();
        nm.NetworkConfig.Prefabs.Add(new NetworkPrefab{Prefab=p});nm.NetworkConfig.PlayerPrefab=p;p.SetActive(false);
    }

    static void BuildRobotPrefab(GameObject managerObject)
    {
        var nm=managerObject.GetComponent<NetworkManager>();var r=GameObject.CreatePrimitive(PrimitiveType.Capsule);r.name="RobotNetworkPrefab";r.tag="Robot";r.transform.localScale=new Vector3(.75f,1.3f,.75f);r.AddComponent<NetworkObject>();var rb=r.AddComponent<Rigidbody>();rb.isKinematic=true;rb.mass=25;r.AddComponent<NetworkTransform>();r.AddComponent<CombatBody>();r.AddComponent<RobotBrain>();nm.NetworkConfig.Prefabs.Add(new NetworkPrefab{Prefab=r});r.SetActive(false);robotPrefab=r;
    }

    static void SpawnRobots(){var nm=NetworkManager.Singleton;if(nm==null||!nm.IsServer||robotPrefab==null)return;foreach(var pos in spawns){var robot=Object.Instantiate(robotPrefab,pos,Quaternion.identity);robot.SetActive(true);robot.GetComponent<NetworkObject>().Spawn();}}
    public static Vector3 GetRobotSpawn()=>spawns[Random.Range(0,spawns.Length)];
    static GameObject MakeBlock(string name,Vector3 pos,Vector3 scale,Color color,string tag,bool physics=false){var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.position=pos;go.transform.localScale=scale;go.GetComponent<Renderer>().material.color=color;if(!string.IsNullOrEmpty(tag))go.tag=tag;if(physics){var rb=go.AddComponent<Rigidbody>();rb.mass=10;}return go;}
    static void MakeWall(Vector3 pos,Vector3 scale,Color color)=>MakeBlock("Wall",pos,scale,color,"ArenaWall");
}
