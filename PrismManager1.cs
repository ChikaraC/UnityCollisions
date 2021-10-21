
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PrismManager : MonoBehaviour
{
    public int prismCount = 10;
    public float prismRegionRadiusXZ = 5;
    public float prismRegionRadiusY = 5;
    public float maxPrismScaleXZ = 5;
    public float maxPrismScaleY = 5;
    public GameObject regularPrismPrefab;
    public GameObject irregularPrismPrefab;

    private List<Prism> prisms = new List<Prism>();
    private List<GameObject> prismObjects = new List<GameObject>();
    private GameObject prismParent;
    private Dictionary<Prism, bool> prismColliding = new Dictionary<Prism, bool>();

    private const float UPDATE_RATE = 0.5f;

    private List<Vector3> scales = new List<Vector3>();
    private List<bbox> bboxes = new List<bbox>();

    struct bbox
    {
        public float x_min;
        public float y_min;
        public float x_max;
        public float y_max;
        public int bboxId;
        public int getbboxIdX(float value)
        {
            if (value == x_min || value == x_max)
            {
                return bboxId;
            }
            else
            {
                return -1;
            }
        }
        public int getbboxIdY(float value)
        {
            if (value == y_min || value == y_max)
            {
                return bboxId;
            }
            else
            {
                return -1;
            }
        }
    };

    struct Idpair
    {
        public int id1;
        public int id2;
    }

    #region Unity Functions

    void Start()
    {
        Random.InitState(0);   

        prismParent = GameObject.Find("Prisms");
        for (int i = 0; i < prismCount; i++)
        {
            var randPointCount = Mathf.RoundToInt(3 + Random.value * 7);
            var randYRot = Random.value * 360;
            var randScale = new Vector3((Random.value - 0.5f) * 2 * maxPrismScaleXZ, (Random.value - 0.5f) * 2 * maxPrismScaleY, (Random.value - 0.5f) * 2 * maxPrismScaleXZ);
            var randPos = new Vector3((Random.value - 0.5f) * 2 * prismRegionRadiusXZ, (Random.value - 0.5f) * 2 * prismRegionRadiusY, (Random.value - 0.5f) * 2 * prismRegionRadiusXZ);
            scales.Add(randScale);
            GameObject prism = null;
            Prism prismScript = null;
            if (Random.value < 0.5f)
            {
                prism = Instantiate(regularPrismPrefab, randPos, Quaternion.Euler(0, randYRot, 0));
                prismScript = prism.GetComponent<RegularPrism>();
            }
            else
            {
                prism = Instantiate(irregularPrismPrefab, randPos, Quaternion.Euler(0, randYRot, 0));
                prismScript = prism.GetComponent<IrregularPrism>();
            }
            prism.name = "Prism " + i;
            prism.transform.localScale = randScale;
            prism.transform.parent = prismParent.transform;
            prismScript.pointCount = randPointCount;
            prismScript.prismObject = prism;

            prisms.Add(prismScript);
            prismObjects.Add(prism);
            prismColliding.Add(prismScript, false);
        }

        StartCoroutine(Run());
    }

    void Update()
    {
        #region Visualization

        DrawPrismRegion();
        DrawPrismWireFrames();

#if UNITY_EDITOR
        if (Application.isFocused)
        {
            UnityEditor.SceneView.FocusWindowIfItsOpen(typeof(UnityEditor.SceneView));
        }
#endif

        #endregion
    }

    IEnumerator Run()
    {
        yield return null;

        while (true)
        {
            foreach (var prism in prisms)
            {
                prismColliding[prism] = false;
            }

            foreach (var collision in PotentialCollisions())
            {
                if (CheckCollision(collision))
                {
                    prismColliding[collision.a] = true;
                    prismColliding[collision.b] = true;

                    ResolveCollision(collision);
                }
            }

            yield return new WaitForSeconds(UPDATE_RATE);
        }
    }
    #endregion

    #region Incomplete Functions

    private IEnumerable<PrismCollision> PotentialCollisions()
    {

        for (int i = 0; i < prisms.Count; i++)
        {
            float x = prisms[i].transform.position.x;
            float z = prisms[i].transform.position.z;
            Vector3 max;
            Vector3 min;
            max = min = prisms[i].points[0];
            for (int k = 0; k < prisms[i].points.Length; k++)
            {
                if (max.x < prisms[i].points[k].x)
                {
                    max.x = prisms[i].points[k].x;
                }
                if (max.z < prisms[i].points[k].z)
                {
                    max.z = prisms[i].points[k].z;
                }
                if (min.x > prisms[i].points[k].x)
                {
                    min.x = prisms[i].points[k].x;
                }
                if (min.z > prisms[i].points[k].z)
                {
                    min.z = prisms[i].points[k].z;
                }
            }
            float minx = min.x;
            float minz = min.z;
            float maxx = max.x;
            float maxz = max.z;
            bbox temp;
            temp.x_min = minx;
            temp.x_max = maxx;
            temp.y_max = maxz;
            temp.y_min = minz;
            temp.bboxId = i;
            if (bboxes.Count < prisms.Count)
            {
                bboxes.Add(temp);
            }


            for (int j = i + 1; j < prisms.Count; j++)
            {
                var checkPrisms = new PrismCollision();
                checkPrisms.a = prisms[i];
                checkPrisms.b = prisms[j];

                yield return checkPrisms;
            }
        }
        List<Idpair> xpairs = new List<Idpair>();
        List<Idpair> ypairs = new List<Idpair>();
        List<Idpair> finalpairs = new List<Idpair>();
        xpairs = SortandSweepX(bboxes);
        ypairs = SortandSweepY(bboxes);

        for (int i = 0; i < xpairs.Count; i++)
        {
            for (int j = 0; j < ypairs.Count; j++)
            {
                if (xpairs[i].id1 == ypairs[j].id1 && xpairs[i].id2 == ypairs[j].id2 ||
                    xpairs[i].id2 == ypairs[j].id1 && xpairs[i].id1 == ypairs[j].id2)
                {
                    finalpairs.Add(xpairs[i]);
                }
            }
        }
        int ic = 10;
        if (ic == 0)
        {

        }
        yield break;
    }
    private List<Idpair> SortandSweepX(List<bbox> _bboxes)
    {
        List<float> Xcoordnate = new List<float>();
        for (int i = 0; i < _bboxes.Count; i++)
        {
            Xcoordnate.Add(_bboxes[i].x_min);
            Xcoordnate.Add(_bboxes[i].x_max);
        }
        Xcoordnate.Sort();
        List<Idpair> collision_pairs_x = new List<Idpair>();
        List<int> activeList = new List<int>();
        int bbox_id = 0;
        int index = 0;
        for (int k = 0; k < Xcoordnate.Count; k++)
        {
            for (int i = 0; i < _bboxes.Count; i++)
            {
                bbox_id = _bboxes[i].getbboxIdX(Xcoordnate[k]);
                if (bbox_id != -1)
                {
                    if (activeList.Contains(bbox_id))
                    {
                        index = activeList.IndexOf(bbox_id);
                        if (activeList[activeList.Count - 1] != bbox_id)
                        {
                            for (int j = index + 1; j < activeList.Count; j++)
                            {
                                Idpair pair;
                                pair.id1 = bbox_id;
                                pair.id2 = activeList[j];
                                collision_pairs_x.Add(pair);
                            }
                        }
                        activeList.Remove(bbox_id);
                    }
                    else
                    {
                        activeList.Add(bbox_id);
                    }

                }

            }
        }
        return collision_pairs_x;
    }

    private List<Idpair> SortandSweepY(List<bbox> _bboxes)
    {
        List<float> Ycoordnate = new List<float>();
        for (int i = 0; i < _bboxes.Count; i++)
        {
            Ycoordnate.Add(_bboxes[i].y_min);
            Ycoordnate.Add(_bboxes[i].y_max);
        }
        Ycoordnate.Sort();
        List<Idpair> collision_pairs_y = new List<Idpair>();
        List<int> activeList = new List<int>();
        int bbox_id = 0;
        int index = 0;
        for (int k = 0; k < Ycoordnate.Count; k++)
        {
            for (int i = 0; i < _bboxes.Count; i++)
            {
                bbox_id = _bboxes[i].getbboxIdY(Ycoordnate[k]);
                if (bbox_id != -1)
                {
                    if (activeList.Contains(bbox_id))
                    {
                        index = activeList.IndexOf(bbox_id);
                        if (activeList[activeList.Count - 1] != bbox_id)
                        {
                            for (int j = index + 1; j < activeList.Count; j++)
                            {
                                Idpair pair;
                                pair.id1 = bbox_id;
                                pair.id2 = activeList[j];
                                collision_pairs_y.Add(pair);
                            }
                        }
                        activeList.Remove(bbox_id);
                    }
                    else
                    {
                        activeList.Add(bbox_id);
                    }

                }

            }
        }
        return collision_pairs_y;
    }
    
    private bool CheckCollision(PrismCollision collision)
    {
        var prismA = collision.a;
        var prismB = collision.b;

        GJK gjk = new GJK();

        if(!gjk.queryCollision(prismA, prismB))
            return false;

        collision.penetrationDepthVectorAB = gjk.penetrationVector;

        return true;
    }
    #endregion

    #region Private Functions

    private void ResolveCollision(PrismCollision collision)
    {
        var prismObjA = collision.a.prismObject;
        var prismObjB = collision.b.prismObject;

        var pushA = -collision.penetrationDepthVectorAB / 2;
        var pushB = collision.penetrationDepthVectorAB / 2;

        for (int i = 0; i < collision.a.pointCount; i++)
        {
            collision.a.points[i] += pushA;
        }
        for (int i = 0; i < collision.b.pointCount; i++)
        {
            collision.b.points[i] += pushB;
        }
       

        Debug.DrawLine(prismObjA.transform.position, prismObjA.transform.position + collision.penetrationDepthVectorAB, Color.cyan, UPDATE_RATE);
    }

    #endregion

    #region Visualization Functions

    private void DrawPrismRegion()
    {
        var points = new Vector3[] { new Vector3(1, 0, 1), new Vector3(1, 0, -1), new Vector3(-1, 0, -1), new Vector3(-1, 0, 1) }.Select(p => p * prismRegionRadiusXZ).ToArray();

        var yMin = -prismRegionRadiusY;
        var yMax = prismRegionRadiusY;

        var wireFrameColor = Color.yellow;

        foreach (var point in points)
        {
            Debug.DrawLine(point + Vector3.up * yMin, point + Vector3.up * yMax, wireFrameColor);
        }

        for (int i = 0; i < points.Length; i++)
        {
            Debug.DrawLine(points[i] + Vector3.up * yMin, points[(i + 1) % points.Length] + Vector3.up * yMin, wireFrameColor);
            Debug.DrawLine(points[i] + Vector3.up * yMax, points[(i + 1) % points.Length] + Vector3.up * yMax, wireFrameColor);
        }
    }

    private void DrawPrismWireFrames()
    {
        for (int prismIndex = 0; prismIndex < prisms.Count; prismIndex++)
        {
            var prism = prisms[prismIndex];
            var prismTransform = prismObjects[prismIndex].transform;

            var yMin = prism.midY - prism.height / 2 * prismTransform.localScale.y;
            var yMax = prism.midY + prism.height / 2 * prismTransform.localScale.y;

            var wireFrameColor = prismColliding[prisms[prismIndex]] ? Color.red : Color.green;

            foreach (var point in prism.points)
            {
                Debug.DrawLine(point + Vector3.up * yMin, point + Vector3.up * yMax, wireFrameColor);
            }

            for (int i = 0; i < prism.pointCount; i++)
            {
                Debug.DrawLine(prism.points[i] + Vector3.up * yMin, prism.points[(i + 1) % prism.pointCount] + Vector3.up * yMin, wireFrameColor);
                Debug.DrawLine(prism.points[i] + Vector3.up * yMax, prism.points[(i + 1) % prism.pointCount] + Vector3.up * yMax, wireFrameColor);
            }
        }
    }

    #endregion

    #region Utility Classes

    private class PrismCollision
    {
        public Prism a;
        public Prism b;
        public Vector3 penetrationDepthVectorAB;
    }

    private class Tuple<K, V>
    {
        public K Item1;
        public V Item2;

        public Tuple(K k, V v)
        {
            Item1 = k;
            Item2 = v;
        }
    }

    #endregion
}
