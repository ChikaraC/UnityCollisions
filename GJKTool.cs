using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class GJKTool
{
    /// 判读点c在ab的哪一侧
    public static int whitchSide(Vector2 a, Vector2 b, Vector2 c)
    {
        Vector2 ab = b - a;
        Vector2 ac = c - a;
        float cross = ab.x * ac.y - ab.y * ac.x;
        return cross > 0 ? 1 : (cross < 0 ? -1 : 0);
    }

    /// 获得原点到线段ab的最近距离。最近距离可以是垂点，也可以是线段的端点。
    public static Vector2 getClosestPointToOrigin(Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        Vector2 ao = Vector2.zero - a;

        float projection = Vector2.Dot(ab, ao) / ab.sqrMagnitude;
        if (projection < 0)
        {
            return a;
        }
        else if (projection > 1.0f)
        {
            return b;
        }
        else
        {
            return a + ab * projection;
        }
    }

    /// 获得原点到直线ab的垂点
    public static Vector2 getPerpendicularToOrigin(Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        Vector2 ao = Vector2.zero - a;

        float projection = Vector2.Dot(ab, ao) / ab.sqrMagnitude;
        return a + ab * projection;
    }

    public static bool contains(List<Vector2> points, Vector2 point)
    {
        int n = points.Count;
        if (n < 3)
        {
            return false;
        }

        // 先计算出内部的方向
        int innerSide = whitchSide(points[0], points[1], points[2]);

        // 通过判断点是否均在三条边的内侧，来判定单形体是否包含点
        for (int i = 0; i < n; ++i)
        {
            int iNext = (i + 1) % n;
            int side = whitchSide(points[i], points[iNext], point);

            if (side == 0) // 在边界上
            {
                return true;
            }

            if (side != innerSide) // 在外部
            {
                return false;
            }
        }

        return true;
    }
}
