using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.XR.ARFoundation;

public class HomeManager : MonoBehaviourPunCallbacks
{
    // 位置合わせ用
    [SerializeField] private ARSessionOrigin sessionOrigin;
    [SerializeField] private ARTrackedImageManager imageManager;
    private GameObject worldOrigin;    // ワールドの原点として振る舞うオブジェクト
    private Coroutine originCoroutine;

    public GameObject arCamera;
    public GameObject myStrPrefab;
    public GameObject othersStrPrefab;

    public float updatePitchInterval = 0.1f; // strPitchの値を更新する間隔
    private WaitForSeconds waitToUpdate;

    private int count;
    private Vector3 prePos;

    public float strRadius = 0.5f;

    public float minDist = 0f;    // 2個目のピンを置くまでに最低限移動する距離
    public float maxRatio = 4.0f;   // 弦が張られる自分のピンと他人のピンの最大距離^2 = maxRatio * sqrInitLength
    public float minRatio = 0.28f;  // 弦が張られる自分のピンと他人のピンの最小距離^2 = minRatio * sqrInitLength
    private float[] harmonies = { 0.36f, 0.44f, 0.56f, 0.64f, 1.0f, 1.44f, 1.78f, 2.25f, 2.56f, 3.16f, 100.0f };

    public GameObject expText;
    public GameObject pitchTextObj;
    public Text pitchText;
    public float front;
    public GameObject checkTracking;

    private float sqrInitLength;
    private float strPitch;

    public Slider lengthSlider;
    public Text sliderValueText;

    // 位置合わせ
    private void OnEnable()
    {
        worldOrigin = new GameObject("Origin");
        Debug.Log("Created origin.");
        imageManager.trackedImagesChanged += OnTrackedImagesChanged;
    }

    private void OnDisable()
    {
        imageManager.trackedImagesChanged -= OnTrackedImagesChanged;
    }

    private IEnumerator OriginDecide(ARTrackedImage trackedImage, float trackInterval)
    {
        yield return new WaitForSeconds(trackInterval);
        var trackedImageTransform = trackedImage.transform;
        worldOrigin.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        sessionOrigin.MakeContentAppearAt(worldOrigin.transform, trackedImageTransform.position, trackedImageTransform.localRotation);
        Debug.Log("Adjusted the origin.");
        originCoroutine = null;
        checkTracking.SetActive(false);
    }

    // ワールド座標を任意の点から見たローカル座標に変換
    public Vector3 WorldToOriginLocal(Vector3 world)    // worldはワールド座標
    {
        return worldOrigin.transform.InverseTransformDirection(world);
    }

    // TrackedImagesChanged時の処理
    private void OnTrackedImagesChanged(ARTrackedImagesChangedEventArgs eventArgs)  // eventArgsは検出イベントに関する引数
    {
        foreach (var trackedImage in eventArgs.added)
        {
            checkTracking.SetActive(true);
            StartCoroutine(OriginDecide(trackedImage, 0));
        }

        foreach (var trackedImage in eventArgs.updated)
        {
            if (originCoroutine == null)
            {
                checkTracking.SetActive(true);
                originCoroutine = StartCoroutine(OriginDecide(trackedImage, 0.5f));
            }
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        // 初期化
        waitToUpdate = new WaitForSeconds(updatePitchInterval);
        count = 0;
        // スライダーの初期化
        float initLength = lengthSlider.value;
        sqrInitLength = initLength * initLength;
        Debug.Log("DebugLog sqrInitLength: " + sqrInitLength);

        // strPitchの値を更新するコルーチンを開始
        StartCoroutine(UpdatePitch());
        Debug.Log("Started UpdatePitch Coroutine.");
    }

    private IEnumerator UpdatePitch()
    {
        while (true)
        {
            Transform camTran = arCamera.transform;
            Vector3 curPos = camTran.position + front * camTran.forward;
            float sqrDist = (curPos - prePos).sqrMagnitude;

            float ratio = sqrDist / sqrInitLength;
            foreach (float harmony in harmonies)
            {
                if (ratio < harmony)
                {
                    // strPitchの値を更新
                    strPitch = harmony;
                    switch (strPitch)
                    {
                        case 0.36f:
                            pitchText.text = "B3";
                            break;
                        case 0.44f:
                            pitchText.text = "A3";
                            break;
                        case 0.56f:
                            pitchText.text = "G3";
                            break;
                        case 0.64f:
                            pitchText.text = "F3";
                            break;
                        case 1.0f:
                            pitchText.text = "E3";
                            break;
                        case 1.44f:
                            pitchText.text = "C3";
                            break;
                        case 1.78f:
                            pitchText.text = "A2";
                            break;
                        case 2.25f:
                            pitchText.text = "G2";
                            break;
                        case 2.56f:
                            pitchText.text = "F2";
                            break;
                        case 3.16f:
                            pitchText.text = "E2";
                            break;
                        case 100.0f:
                            pitchText.text = "D2";
                            break;
                        default:
                            pitchText.text = "";
                            break;
                    }
                    break;
                }
            }
            if (count > 0)
            {
                expText.SetActive(true);
                pitchTextObj.SetActive(true);
            }
            yield return waitToUpdate;
        }
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void OnLengthSliderValueChanged()
    {
        float initLength = lengthSlider.value;
        sqrInitLength = initLength * initLength;
        Debug.Log("DebugLog sqrInitLength: " + sqrInitLength);

        sliderValueText.text = Mathf.Floor(initLength * 100).ToString() + "cm";
    }

    public void OnClickMarkerButton()
    {
        Debug.Log("Clicked Marker Button.");

        // ピンを配置
        Transform camTran = arCamera.transform;
        Vector3 curPos = camTran.position + front * camTran.forward;
        // PhotonNetwork.Instantiate("Marker1", curPos, Quaternion.Euler(90f, 0f, 0f));
        PhotonNetwork.Instantiate("Marker1", curPos, Quaternion.identity);
        Debug.Log("Placed a marker.");

        // 2つ目以降のピンなら弦を張る
        if (count > 0)
        {
            // curPosは置いたばかりのピンの座標でprePosは1つ前のピンの座標
            Vector3 strVec = curPos - prePos;           // 弦の方向を取得
            float dist = strVec.magnitude;              // 弦の長さを取得
            Vector3 strY = new Vector3(0f, dist, 0f);
            Vector3 halfStrVec = strVec * 0.5f;
            Vector3 centerCoord = prePos + halfStrVec;  // 弦の中点の座標        
            // myStrPrefabは弦（Capsule）のプレハブ
            GameObject str = Instantiate(myStrPrefab, centerCoord, Quaternion.identity);    // 弦をインスタンス化
            // strRadiusは弦（Capsule）の太さ
            str.transform.localScale = new Vector3(strRadius, dist / 2, strRadius);         // ひとまず弦をY軸方向に伸ばす

            CapsuleCollider col = str.GetComponent<CapsuleCollider>();
            col.isTrigger = true;   // 衝突判定を行わないように
            str.transform.rotation = Quaternion.FromToRotation(strY, strVec);   // 弦を本来の方向に回転

            // 弦にプロパティ(pitch)を追加
            StringController stringController = str.GetComponent<StringController>();
            stringController.pitch = strPitch;
        }
        // ピンの座標を保存
        prePos = curPos;
        count += 1;
        Debug.Log("count: " + count.ToString());
        // 他人のピンと弦を張る
        DisplayStrWithOther(prePos);
    }

    private void DisplayStrWithOther(Vector3 myMarkerPos)
    {
        GameObject[] markers = GameObject.FindGameObjectsWithTag("Marker"); // ピンを全取得
        Debug.Log("Acquired all markers.");
        foreach (GameObject marker in markers)
        {
            Debug.Log("Checking one marker.");
            PhotonView markerPhotonView = marker.GetComponent<PhotonView>();
            if (!markerPhotonView.IsMine)
            {
                Debug.Log("This marker is not mine.");
                Vector3 othersMarkerPos = marker.transform.position;
                float sqrDist = (othersMarkerPos - myMarkerPos).sqrMagnitude;
                float ratio = sqrDist / sqrInitLength;
                if (minRatio < ratio && ratio < maxRatio)
                {
                    foreach (float harmony in harmonies)
                    {
                        if (ratio < harmony)
                        {
                            // 弦をコライダーと共に設置
                            Vector3 strVec = othersMarkerPos - myMarkerPos; // 弦の方向を取得
                            float dist = strVec.magnitude;  // 弦の長さを取得
                            Vector3 strY = new Vector3(0f, dist, 0f);
                            Vector3 halfStrVec = strVec * 0.5f;
                            Vector3 centerCoord = myMarkerPos + halfStrVec;    // 中点の座標        

                            GameObject str = Instantiate(othersStrPrefab, centerCoord, Quaternion.identity) as GameObject;
                            Debug.Log("Instantiated othersStr");
                            str.transform.localScale = new Vector3(strRadius, dist / 2, strRadius); // ひとまずY軸方向に伸ばす

                            CapsuleCollider col = str.GetComponent<CapsuleCollider>();
                            col.isTrigger = true;   // 衝突判定を行わないように
                            str.transform.rotation = Quaternion.FromToRotation(strY, strVec);   // 弦を回転

                            // 弦にプロパティ(pitch)を追加
                            StringController stringController = str.GetComponent<StringController>();
                            stringController.pitch = harmony;

                            break;
                        }
                    }
                }
            }
        }
    }

    public void OnClickTrashButton()
    {
        Debug.Log("Clicked Trash Button.");
        expText.SetActive(false);
        pitchText.text = "";
        pitchTextObj.SetActive(false);        
        count = 0;
        // sqrInitLength = 0;
        float initLength = lengthSlider.value;
        sqrInitLength = initLength * initLength;

        GameObject[] markers = GameObject.FindGameObjectsWithTag("Marker");
        foreach (GameObject marker in markers)
        {
            Destroy(marker);
        }
        Debug.Log("Deleted all my markers.");

        GameObject[] myStrs = GameObject.FindGameObjectsWithTag("MyString");
        foreach (GameObject myStr in myStrs)
        {
            Destroy(myStr);
        }
        GameObject[] othersStrs = GameObject.FindGameObjectsWithTag("OthersString");
        foreach (GameObject othersStr in othersStrs)
        {
            Destroy(othersStr);
        }        
        Debug.Log("Deleted all strings.");
    }
}
