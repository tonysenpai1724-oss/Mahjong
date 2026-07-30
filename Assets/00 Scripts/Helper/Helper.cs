using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using TriangleNet.Geometry;
using TriangleNet;
using UnityEditor;
using UnityEngine;
using Color = UnityEngine.Color;
using Random = UnityEngine.Random;
using System.IO.Compression;
using System.Text;
using System.Linq;
using TriangleNet.Topology;
using JWT.Algorithms;
using JWT.Serializers;
using JWT;
using Newtonsoft.Json;

public static class Helper
{
    public static JwtPayload ParseJwt(string jwtToken)
    {
        var jsonSerializer = new JsonNetSerializer();
        var dateTimeProvider = new UtcDateTimeProvider();
        var urlEncoder = new JwtBase64UrlEncoder();
        var algorithm = new HMACSHA256Algorithm();
        var validator = new JwtValidator(jsonSerializer, dateTimeProvider);

        var jwtDecoder = new JwtDecoder(jsonSerializer, validator, urlEncoder, algorithm);
        try
        {
            string payload = jwtDecoder.Decode(jwtToken, false);
            DebugCustom.LogColor(payload);
            return JsonConvert.DeserializeObject<JwtPayload>(payload);
        }
        catch (Exception ex)
        {
            Debug.LogError("Invalid token: " + ex.Message);
        }

        return null;
    }
    public static bool IsPointInPolygon(Vector2 point, Mesh mesh, Vector2 position, float rotation)
    {
        // Transform the point to the local space of the mesh
        Quaternion rotationQuat = Quaternion.Euler(0, 0, -rotation);
        Vector2 localPoint = rotationQuat * (point - position);

        // Check if the point is inside the Mesh
        return IsPointInMesh(localPoint, mesh);
    }

    private static bool IsPointInMesh(Vector2 point, Mesh mesh)
    {
        Vector3[] vertices = mesh.vertices;
        int[] triangles = mesh.triangles;

        for (int i = 0; i < triangles.Length; i += 3)
        {
            Vector3 v0 = vertices[triangles[i]];
            Vector3 v1 = vertices[triangles[i + 1]];
            Vector3 v2 = vertices[triangles[i + 2]];

            if (IsPointInTriangle(point, v0, v1, v2))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsPointInTriangle(Vector2 p, Vector3 a, Vector3 b, Vector3 c)
    {
        float sign(Vector2 v1, Vector2 v2, Vector2 v3)
        {
            return (v1.x - v3.x) * (v2.y - v3.y) - (v2.x - v3.x) * (v1.y - v3.y);
        }

        bool b1, b2, b3;

        b1 = sign(p, a, b) < 0.0f;
        b2 = sign(p, b, c) < 0.0f;
        b3 = sign(p, c, a) < 0.0f;

        return ((b1 == b2) && (b2 == b3));
    }
    public static DateTime NextDay(this DateTime dateTime)
    {
        return dateTime.Date.AddDays(1); // Ngày tiếp theo, lúc 00:00:00
    }

    public static DateTime NextWeek(this DateTime dateTime)
    {
        int daysUntilNextMonday = ((int)DayOfWeek.Monday - (int)dateTime.DayOfWeek + 7) % 7;
        return dateTime.Date.AddDays(daysUntilNextMonday == 0 ? 7 : daysUntilNextMonday); // Thứ Hai tuần sau, 00:00:00
    }

    public static DateTime NextMonth(this DateTime dateTime)
    {
        DateTime nextMonth = dateTime.AddMonths(1);
        return new DateTime(nextMonth.Year, nextMonth.Month, 1); // Ngày 1 tháng tiếp theo, 00:00:00
    }
    public static string GetTextResourceValue(long val)
    {
        List<string> lstSubfix = new List<string>
        {
            "", "K", "M", "B", "T", "E", "D", "A", "X", "Y", "Z",
        };

        double startMultiple = 1e3;
        string txt = "0";
        double _val = val;
        int idSubfix = 0;
        while (_val > startMultiple - 1)
        {
            _val /= startMultiple;
            idSubfix++;
        }

        txt = _val.ToString("0.##");
        if (txt.Length > 4)
            txt = txt.Remove(4);
        if (txt.EndsWith(","))
            txt = txt.Replace(",", "");
        if (txt.EndsWith("."))
            txt = txt.Replace(".", "");
        txt += lstSubfix[idSubfix];
        return txt;
    }

    public static string DecompressFromBase64GzipData(string base64Data)
    {
        if (string.IsNullOrEmpty(base64Data))
            return "";
        try
        {
            byte[] compressedBytes = Convert.FromBase64String(base64Data);
            using (var inputStream = new MemoryStream(compressedBytes))
            using (var gzipStream = new GZipStream(inputStream, CompressionMode.Decompress))
            using (var reader = new StreamReader(gzipStream, Encoding.UTF8))
            {
                return reader.ReadToEnd();
            }
        }
        catch
        {
            Debug.LogError($"Cannot Parse GzipData :\n{base64Data}");
            return string.Empty;
        }
    }

    public static string CompressToBase64GzipData(string jsonData)
    {
        byte[] jsonBytes = Encoding.UTF8.GetBytes(jsonData);
        using (var outputStream = new MemoryStream())
        {
            using (var gzipStream = new GZipStream(outputStream, CompressionMode.Compress))
            {
                gzipStream.Write(jsonBytes, 0, jsonBytes.Length);
            }
            return Convert.ToBase64String(outputStream.ToArray());
        }
    }
    public static Mesh GetMesh(List<List<Vector2>> contours)
    {
        Polygon poly = new Polygon();
        for (int i = 0; i < contours.Count; i++)
        {
            poly.Add(contours[i], i > 0);
        }
        var triangleNetMesh = (TriangleNetMesh)poly.Triangulate();
        var mesh = triangleNetMesh.GenerateUnityMesh();
        mesh.uv = Helper.GenerateUv(mesh.vertices);
        return mesh;
    }
    public static bool IsParallel(List<Vector2Int> lst)
    {
        List<Vector2Int> _lst = new List<Vector2Int>(lst);
        foreach (var item in lst)
        {
            if (_lst.Contains(new Vector2Int(-item.x, item.y)))
                _lst.Remove(item);
        }
        return _lst.Count == 0;
    }
    public static Vector2 GetInLevelPosition(Vector2Int nodePos, int rotation)
    {
        return Quaternion.Inverse(Quaternion.Euler(0, 0, rotation)) * (Vector2)(nodePos);
    }
    public static Vector2 GetReflect(Vector2 point, Vector2 a, Vector2 b)
    {
        // Vector chỉ phương của đường thẳng AB
        Vector2 abDir = (b - a).normalized;

        // Vector từ A đến point
        Vector2 ap = point - a;

        // Chiếu vector AP lên đường thẳng AB
        float projectionLength = Vector2.Dot(ap, abDir);
        Vector2 projection = abDir * projectionLength;

        // Tính điểm trên đường thẳng gần nhất với point (hình chiếu)
        Vector2 nearestPoint = a + projection;

        // Tính điểm đối xứng qua nearestPoint
        Vector2 reflectedPoint = nearestPoint * 2 - point;

        return reflectedPoint;
    }
    public static List<Vector2> GetHShapeContour(float x, float y, float corner)
    {
        List<Vector2> contour = new List<Vector2>();
        float thickness = Constant.barWidth;
        float halfW = x / 2;
        float halfH = y / 2;
        float halfT = thickness / 2;

        // Xác định các điểm góc chính của hình chữ H
        Vector2 topLeft = new Vector2(-halfW, halfH);
        Vector2 topRight = new Vector2(halfW, halfH);
        Vector2 midTopLeft = new Vector2(-halfT, halfH - corner);
        Vector2 midTopRight = new Vector2(halfT, halfH - corner);

        Vector2 midBottomLeft = new Vector2(-halfT, -halfH + corner);
        Vector2 midBottomRight = new Vector2(halfT, -halfH + corner);
        Vector2 bottomLeft = new Vector2(-halfW, -halfH);
        Vector2 bottomRight = new Vector2(halfW, -halfH);

        // Bắt đầu đi viền theo ngược chiều kim đồng hồ
        contour.Add(topLeft);
        contour.AddRange(Helper.GetCircleVertics(topLeft, corner, topLeft, new Vector2(-halfW + corner, halfH))); // Bo góc trên trái
        contour.Add(new Vector2(-halfW + corner, halfH));

        contour.Add(new Vector2(-halfT, halfH));
        contour.AddRange(Helper.GetCircleVertics(midTopLeft, corner, new Vector2(-halfT, halfH), midTopLeft)); // Bo góc vào thanh ngang trên
        contour.Add(midTopLeft);

        contour.Add(midTopRight);
        contour.AddRange(Helper.GetCircleVertics(new Vector2(halfT, halfH), corner, midTopRight, new Vector2(halfT, halfH))); // Bo góc ra thanh ngang trên
        contour.Add(new Vector2(halfT, halfH));

        contour.Add(new Vector2(halfW - corner, halfH));
        contour.AddRange(Helper.GetCircleVertics(topRight, corner, new Vector2(halfW - corner, halfH), topRight)); // Bo góc trên phải
        contour.Add(topRight);

        contour.Add(bottomRight);
        contour.AddRange(Helper.GetCircleVertics(bottomRight, corner, bottomRight, new Vector2(halfW - corner, -halfH))); // Bo góc dưới phải
        contour.Add(new Vector2(halfW - corner, -halfH));

        contour.Add(new Vector2(halfT, -halfH));
        contour.AddRange(Helper.GetCircleVertics(midBottomRight, corner, new Vector2(halfT, -halfH), midBottomRight)); // Bo góc vào thanh ngang dưới
        contour.Add(midBottomRight);

        contour.Add(midBottomLeft);
        contour.AddRange(Helper.GetCircleVertics(new Vector2(-halfT, -halfH), corner, midBottomLeft, new Vector2(-halfT, -halfH))); // Bo góc ra thanh ngang dưới
        contour.Add(new Vector2(-halfT, -halfH));

        contour.Add(new Vector2(-halfW + corner, -halfH));
        contour.AddRange(Helper.GetCircleVertics(bottomLeft, corner, new Vector2(-halfW + corner, -halfH), bottomLeft)); // Bo góc dưới trái
        contour.Add(bottomLeft);

        return contour;
    }

    public static List<Vector2> GetPlusShapeContour(float lenght, float conner)
    {
        float a = Constant.barWidth / 2;
        float b = lenght / 2;
        float a1 = a + conner;
        float a2 = a - conner;
        float b1 = b + conner;
        float b2 = b - conner;
        List<Vector2> contours = new List<Vector2>();

        contours.AddRange(GetCircleVertics(new Vector2(b2, -a2), conner, new Vector2(b2, -a), new Vector2(b, -a2)));
        contours.AddRange(GetCircleVertics(new Vector2(b2, a2), conner, new Vector2(b, a2), new Vector2(b2, a)));

        List<Vector2> c1 = GetCircleVertics(new Vector2(a1, a1), conner, new Vector2(a, a1), new Vector2(a1, a));
        c1.Reverse();
        contours.AddRange(c1);

        contours.AddRange(GetCircleVertics(new Vector2(a2, b2), conner, new Vector2(a, b2), new Vector2(a2, b)));
        contours.AddRange(GetCircleVertics(new Vector2(-a2, b2), conner, new Vector2(-a2, b), new Vector2(-a, b2)));

        List<Vector2> c2 = GetCircleVertics(new Vector2(-a1, a1), conner, new Vector2(-a1, a), new Vector2(-a, a1));
        c2.Reverse();
        contours.AddRange(c2);

        contours.AddRange(GetCircleVertics(new Vector2(-b2, a2), conner, new Vector2(-b2, a), new Vector2(-b, a2)));
        contours.AddRange(GetCircleVertics(new Vector2(-b2, -a2), conner, new Vector2(-b, -a2), new Vector2(-b2, -a)));

        List<Vector2> c3 = GetCircleVertics(new Vector2(-a1, -a1), conner, new Vector2(-a, -a1), new Vector2(-a1, -a));
        c3.Reverse();
        contours.AddRange(c3);

        contours.AddRange(GetCircleVertics(new Vector2(-a2, -b2), conner, new Vector2(-a, -b2), new Vector2(-a2, -b)));
        contours.AddRange(GetCircleVertics(new Vector2(a2, -b2), conner, new Vector2(a2, -b), new Vector2(a, -b2)));

        List<Vector2> c4 = GetCircleVertics(new Vector2(a1, -a1), conner, new Vector2(a1, -a), new Vector2(a, -a1));
        c4.Reverse();
        contours.AddRange(c4);


        return contours;

    }
    public static List<Vector2> GetUShapeContour(float x, float y, float conner)
    {
        float z = Constant.barWidth;
        x /= 2;
        float _x = x - conner;
        float _y = y - conner * 2;
        float x1 = x - z;
        float a = z / 2;
        float b = a - conner;
        List<Vector2> contours = new List<Vector2>();

        contours.AddRange(GetCircleVertics(new Vector2(_x, -b), conner, new Vector2(_x, -a), new Vector2(x, -b)));
        contours.AddRange(GetCircleVertics(new Vector2(_x, _y), conner, new Vector2(x, _y), new Vector2(_x, y)));
        contours.AddRange(GetCircleVertics(new Vector2(x1 + conner, _y), conner, new Vector2(x1 + conner, y), new Vector2(x1, _y)));

        List<Vector2> a1 = GetCircleVertics(new Vector2(x1 - conner, a + conner), conner, new Vector2(x1 - conner, a), new Vector2(x1, a + conner));
        a1.Reverse();
        contours.AddRange(a1);
        List<Vector2> a2 = GetCircleVertics(new Vector2(-x1 + conner, a + conner), conner, new Vector2(-x1, a + conner), new Vector2(-x1 + conner, a));
        a2.Reverse();
        contours.AddRange(a2);
        contours.AddRange(GetCircleVertics(new Vector2(-(x1 + conner), _y), conner, new Vector2(-x1, _y), new Vector2(-x1 - conner, y)));
        contours.AddRange(GetCircleVertics(new Vector2(-_x, _y), conner, new Vector2(-_x, y), new Vector2(-x, _y)));
        contours.AddRange(GetCircleVertics(new Vector2(-_x, -b), conner, new Vector2(-x, -b), new Vector2(-_x, -a)));

        return contours;

    }
    public static List<Vector2> GetTShapeContour(float x, float y, float conner)
    {
        float z = Constant.barWidth;
        x /= 2;
        float _x = x - conner;
        float _y = y - conner * 2;
        float a = z / 2;
        float b = a - conner;
        float c = a + conner;
        List<Vector2> contours = new List<Vector2>();

        contours.AddRange(GetCircleVertics(new Vector2(_x, -b), conner, new Vector2(_x, -a), new Vector2(x, -b)));
        contours.AddRange(GetCircleVertics(new Vector2(_x, b), conner, new Vector2(x, b), new Vector2(_x, a)));
        List<Vector2> a1 = GetCircleVertics(new Vector2(c, c), conner, new Vector2(a, c), new Vector2(c, a));
        a1.Reverse();
        contours.AddRange(a1);
        contours.AddRange(GetCircleVertics(new Vector2(b, _y), conner, new Vector2(a, _y), new Vector2(b, y)));
        contours.AddRange(GetCircleVertics(new Vector2(-b, _y), conner, new Vector2(-b, y), new Vector2(-a, _y)));
        List<Vector2> a2 = GetCircleVertics(new Vector2(-c, c), conner, new Vector2(-c, a), new Vector2(-a, c));
        a2.Reverse();
        contours.AddRange(a2);
        contours.AddRange(GetCircleVertics(new Vector2(-_x, b), conner, new Vector2(-_x, a), new Vector2(-x, b)));
        contours.AddRange(GetCircleVertics(new Vector2(-_x, -b), conner, new Vector2(-x, -b), new Vector2(-_x, -a)));

        return contours;

    }
    public static List<Vector2> GetLShapeContour(float x, float y, float conner)
    {
        float z = Constant.barWidth;
        float _x = x - conner * 2;
        float _y = y - conner * 2;
        float a = z / 2;
        float b = a - conner;
        float c = a + conner;

        List<Vector2> contours = new List<Vector2>();

        contours.AddRange(GetCircleVertics(new Vector2(_x, -b), conner, new Vector2(_x, -a), new Vector2(x, -b)));
        contours.AddRange(GetCircleVertics(new Vector2(_x, b), conner, new Vector2(x, b), new Vector2(_x, a)));
        List<Vector2> a1 = GetCircleVertics(new Vector2(c, c), conner, new Vector2(a, c), new Vector2(c, a));
        a1.Reverse();
        contours.AddRange(a1);
        contours.AddRange(GetCircleVertics(new Vector2(b, _y), conner, new Vector2(a, _y), new Vector2(b, y)));
        contours.AddRange(GetCircleVertics(new Vector2(-b, _y), conner, new Vector2(-b, y), new Vector2(-a, _y)));
        contours.AddRange(GetCircleVertics(new Vector2(-b, -b), conner, new Vector2(-a, -b), new Vector2(-b, -a)));

        return contours;

    }
    public static List<Vector2> GetTriangleContour(Vector2 a, Vector2 b, Vector2 c, float corner)
    {
        List<Vector2> _contour = new List<Vector2>();

        // Tính toán các vector hướng giữa các cạnh tam giác
        Vector2 abDir = (b - a).normalized; // Hướng từ a đến b
        Vector2 bcDir = (c - b).normalized; // Hướng từ b đến c
        Vector2 caDir = (a - c).normalized; // Hướng từ c đến a


        // Điểm bo góc trên các cạnh
        Vector2 abaConner = a + abDir * corner; // Điểm trên cạnh AB, cách đỉnh A một đoạn corner
        Vector2 abbConner = b - abDir * corner;   // Điểm trên cạnh AB, cách đỉnh B một đoạn corner

        Vector2 bcbConner = b + bcDir * corner; // Điểm trên cạnh BC, cách đỉnh B một đoạn corner
        Vector2 bccConner = c - bcDir * corner;   // Điểm trên cạnh BC, cách đỉnh C một đoạn corner

        Vector2 cacConner = c + caDir * corner; // Điểm trên cạnh CA, cách đỉnh C một đoạn corner
        Vector2 caaConner = a - caDir * corner;   // Điểm trên cạnh CA, cách đỉnh A một đoạn corner

        Vector2 aCenter = GetReflect(a, abaConner, caaConner);
        Vector2 bCenter = GetReflect(b, abbConner, bcbConner);
        Vector2 cCenter = GetReflect(c, bccConner, cacConner);

        _contour.AddRange(Helper.GetCircleVertics(bCenter, corner, abbConner, bcbConner)); // Bo góc tại đỉnh B

        _contour.AddRange(Helper.GetCircleVertics(cCenter, corner, bccConner, cacConner)); // Bo góc tại đỉnh C

        _contour.AddRange(Helper.GetCircleVertics(aCenter, corner, caaConner, abaConner)); // Bo góc tại đỉnh A

        return _contour;
    }

    public static List<Vector2> GetBoxContour(float x, float y, float conner)
    {
        x /= 2;
        y /= 2;
        List<Vector2> _contour = new List<Vector2>();
        float _x = x - conner;
        float _y = y - conner;

        //_contour.Add(new Vector2(-_x, -y));
        //_contour.Add(new Vector2(_x, -y));
        _contour.AddRange(Helper.GetCircleVertics(new Vector2(_x, -_y), conner, new Vector2(_x, -y), new Vector2(x, -_y)));
        //_contour.Add(new Vector2(x, -_y));
        //_contour.Add(new Vector2(x, _y));
        _contour.AddRange(Helper.GetCircleVertics(new Vector2(_x, _y), conner, new Vector2(x, _y), new Vector2(_x, y)));
        //_contour.Add(new Vector2(_x, y));
        //_contour.Add(new Vector2(-_x, y));
        _contour.AddRange(Helper.GetCircleVertics(new Vector2(-_x, _y), conner, new Vector2(-_x, y), new Vector2(-x, _y)));
        //_contour.Add(new Vector2(-x, _y));
        //_contour.Add(new Vector2(-x, -_y));
        _contour.AddRange(Helper.GetCircleVertics(new Vector2(-_x, -_y), conner, new Vector2(-x, -_y), new Vector2(-_x, -y)));
        return _contour;
    }
    /// <summary>
    /// GetCircleVertics By Reverse Clock Wise Circle
    /// </summary>
    /// <param name="center"></param>
    /// <param name="radius"></param>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <returns></returns>
    public static List<Vector2> GetCircleVertics(Vector2 center, float radius, Vector2 start, Vector2 end)
    {
        List<Vector2> verts = new List<Vector2>();
        float startAngle = 0;
        float endAngle = 359;
        float step = 12f;
        start -= center;
        end -= center;
        if (end != start)
        {
            startAngle = Get2DAngle(start);
            endAngle = Get2DAngle(end);
        }
        startAngle = NormalizeAngleTo(startAngle, endAngle);
        while (startAngle < endAngle)
        {
            startAngle += step;
            Vector2 pos = GetPositionFromAngle(center, startAngle, radius);
            if (Vector2.SqrMagnitude(pos - end) > 0.01f)
                verts.Add(pos);
            else
                break;
        }
        return verts;
    }
    /// <summary>
    /// Chuyển đổi góc a thành góc lượng giác nhỏ nhất và gần b nhất theo đường tròn lượng giác.
    /// </summary>
    /// <param name="a">Góc cần chuẩn hóa (tham chiếu).</param>
    /// <param name="b">Góc tham chiếu.</param>
    public static float NormalizeAngleTo(float a, float b)
    {
        float delta = Mathf.DeltaAngle(a, b);
        return b - (delta >= 0 ? delta : 360 + delta);
    }
    public static Vector2[] GenerateUv(Vector3[] vertices, float uvScale = 0.5f)
    {
        Vector2[] uvs = new Vector2[vertices.Length];
        for (int i = 0; i < vertices.Length; i++)
        {
            uvs[i] = new Vector2(vertices[i].x * uvScale, vertices[i].y * uvScale);
        }

        return uvs;
    }
    public static string FormatSortString(string input)
    {
        if (string.IsNullOrEmpty(input) || input.Length <= 6)
        {
            return input;
        }

        string start = input.Substring(0, 2);
        string end = input.Substring(input.Length - 5, 5);

        return $"{start}...{end}";
    }
    public static int GetDiscountPercent(EDiscount eDiscount)
    {
        switch (eDiscount)
        {
            case EDiscount.E10:
                return 10;
            case EDiscount.E20:
                return 20;
            case EDiscount.E30:
                return 30;
            case EDiscount.E50:
                return 50;
            case EDiscount.E75:
                return 75;
            default:
                break;
        }
        return 0;
    }
    public static T TryGetValue<T>(this List<T> lst, int id)
    {
        if (id < 0 || id >= lst.Count)
            return default(T);
        return lst[id];
    }
    public static List<string> GetSubStats(string str)
    {
        string[] split = str.Split("}{");
        List<string> result = new List<string>();
        foreach (string s in split)
        {
            result.Add(s.Replace("{", "").Replace("}", ""));
        }
        return result;
    }
    public static int RandomDirection()
    {
        int i = Random.Range(0, 2);
        if (i == 0)
            return 1;
        else
            return -1;
    }
    public static Vector2 GetRecoilDirection(this Vector2 baseDirection, float angle)
    {
        if (Mathf.Approximately(angle, 0))
        {
            return baseDirection;
        }

        return GetDirectionFromAngle(Get2DAngle(baseDirection) + Random.Range(-angle / 2, angle / 2));
    }

    public static bool ConditionExcute(float rate)
    {
        return Random.Range(0f, 100f) < rate;
    }

    public static float GetDistanceOnVector(Vector2 a, Vector2 b, Vector2 dir)
    {
        dir = dir.normalized;

        Vector2 perpDir = new Vector2(-dir.y, dir.x);

        Vector2 a1 = a + Vector2.Dot(a, perpDir) * perpDir;

        Vector2 b1 = b + Vector2.Dot(b, perpDir) * perpDir;

        float distance = Vector2.Distance(a1, b1);

        return distance;
    }
    public static bool Equivalent(this Color _color, Color _target)
    {
        return Mathf.Abs(_color.r - _target.r) < 0.1f && Mathf.Abs(_color.g - _target.g) < 0.1f &&
               Mathf.Abs(_color.b - _target.b) < 0.1f && Mathf.Abs(_color.a - _target.a) < 0.1f;
    }

    public static List<Vector3> ConvertToVector3(this List<Vector2> lstV2)
    {
        if (lstV2 == null)
            return null;
        List<Vector3> result = new List<Vector3>();
        foreach (var v in lstV2)
        {
            result.Add(v);
        }

        return result;
    }

    public static bool Contains(this LayerMask mask, int layer)
    {
        return mask == (mask | (1 << layer));
    }

    public static List<T> GetRandom<T>(this List<T> lstT, int numb, bool ignore = true)
    {
        if (lstT.Count == 0 || numb == 0) return new List<T>();
        if (ignore)
        {
            if (lstT.Count < numb)
            {
                DebugCustom.LogError("invalid number needed");
                return new List<T>();
            }
            else
            {
                List<T> lst = new List<T>(lstT);
                List<T> lstResult = new List<T>();
                for (int i = 0; i < numb; i++)
                {
                    T t = lst.GetRandom();
                    lst.Remove(t);
                    lstResult.Add(t);
                }

                return lstResult;
            }
        }
        else
        {
            List<T> lstResult = new List<T>();
            for (int i = 0; i < numb; i++)
            {
                T t = lstT.GetRandom();
                lstResult.Add(t);
            }

            return lstResult;
        }
    }

    public static T GetRandom<T>(this List<T> lstT)
    {
        if (lstT.Count == 0)
            return default(T);
        return lstT[Random.Range(0, lstT.Count)];
    }

    public static T SerializedCoppy<T>(this T _class) where T : class
    {
        string json = Newtonsoft.Json.JsonConvert.SerializeObject(_class);
        return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(json);
    }

    public static List<Vector2Int> GetLineCross(Vector2Int start, Vector2Int end)
    {
        // Check Logic
        //y = mx +b;
        List<Vector2Int> line = new List<Vector2Int>();
        List<Vector2> dir = new List<Vector2>() { Vector2.down, Vector2.up, Vector2.left, Vector2.right };
        int sx = end.x > start.x ? 1 : -1;
        int sy = end.y > start.y ? 1 : -1;
        float dx = Mathf.Abs(end.x - start.x);
        float dy = Mathf.Abs(end.y - start.y);
        line.Add(start);
        if (dx == 0 && dy == 0)
        {
            return line;
        }

        if (dx == 0)
        {
            int startY = start.y;
            while (startY != end.y)
            {
                startY += sy;
                Vector2Int pos = new Vector2Int(start.x, startY);
                if (!line.Contains(pos))
                    line.Add(pos);
            }

            line.Add(end);
            return line;
        }

        if (dy == 0)
        {
            int startX = start.x;
            while (startX != end.x)
            {
                startX += sx;
                Vector2Int pos = new Vector2Int(startX, start.y);
                if (!line.Contains(pos))
                    line.Add(pos);
            }

            line.Add(end);
            return line;
        }

        float m = (float)(end.y - start.y) / (float)(end.x - start.x);
        float b = start.y - m * start.x;

        float getY(float x)
        {
            return m * x + b;
        }

        float step = 0;
        while (step < dx)
        {
            step += 0.1f;
            float x = start.x + sx * step;
            float y = getY(x);
            Vector2Int pos = new Vector2Int(Mathf.RoundToInt(x), Mathf.RoundToInt(y));
            if (!line.Contains(pos))
                line.Add(pos);
            for (int i = 0; i < dir.Count; i++)
            {
                Vector2 _post2 = new Vector2(x, y) + dir[i] * 0.1f;
                Vector2Int _post = new Vector2Int(Mathf.RoundToInt(_post2.x), Mathf.RoundToInt(_post2.y));
                if (!line.Contains(_post))
                    line.Add(_post);
            }
        }

        line.Add(end);
        return line;
    }

    public static Vector2Int ToSimpleDirection(Vector2Int input)
    {
        Vector2 nor = ((Vector2)input).normalized;
        return new Vector2Int(Mathf.RoundToInt(nor.x), Mathf.RoundToInt(nor.y));
    }

    public static bool Contains<T>(this List<T> list, List<T> lstE)
    {
        for (int i = 0; i < lstE.Count; i++)
        {
            if (!list.Contains(lstE[i]))
                return false;
        }

        return true;
    }

    public static void MoveToFront<T>(this List<T> list, T element)
    {
        if (!list.Contains(element))
        {
            Debug.LogError("Element not inside List");
        }
        else
        {
            list.Remove(element);
            list.Insert(0, element);
        }
    }

    public static Vector2 GetClosestPointOnRect(this Rect rect, Vector2 point)
    {
        float clampedX = Mathf.Clamp(point.x, rect.xMin, rect.xMax);
        float clampedY = Mathf.Clamp(point.y, rect.yMin, rect.yMax);

        return new Vector2(clampedX, clampedY);
    }

    public static bool RayCastUnit(Vector3 pos)
    {
        var hit = Physics2D.Raycast(pos, Vector2.zero);
        if (hit.collider != null)
            return true;
        return false;
    }

    //public static List<GameUnit> CastUnitCircle(Vector3 pos, float radius, bool isPlayer)
    //{
    //    List<GameUnit> lstUnit = new List<GameUnit>();
    //    var overlap = Physics2D.OverlapCircleAll(pos, radius, LayerMask.GetMask("Unit", "Building"));
    //    foreach (var item in overlap)
    //    {
    //        GameUnit unit = item.GetComponent<GameUnit>();
    //        if(unit != null)
    //            lstUnit.Add(unit);
    //    }
    //    return lstUnit;
    //}
    public static Vector2 GetCenter(List<Vector2Int> lstPos)
    {
        if (lstPos.Count == 1)
            return lstPos[0];
        else if (lstPos.Count > 1)
        {
            //DebugCustom.LogColorJson(lstPos);
            float x = 0;
            float y = 0;
            for (int i = 0; i < lstPos.Count; i++)
            {
                x += lstPos[i].x;
                y += lstPos[i].y;
            }

            //DebugCustom.LogColorJson(x, y);
            return new Vector2(x / lstPos.Count, y / lstPos.Count);
        }

        return Vector2.zero;
    }

    public static Vector2Int GetVector2Int(Vector3Int v3)
    {
        return new Vector2Int(v3.x, v3.y);
    }

    public static List<Vector2Int> GetFourPairGrid(Vector2 pos)
    {
        List<Vector2Int> result = new List<Vector2Int>();
        Vector2 posR = new Vector2(Mathf.RoundToInt(pos.x), Mathf.RoundToInt(pos.y));
        Vector2 _pos = posR + (pos - posR).normalized * 0.5f;
        //Vector2 _pos = new Vector2((float)(Mathf.RoundToInt(pos.x * 2)) / 2f, (float)(Mathf.RoundToInt(pos.y * 2)) / 2f);
        //DebugCustom.Log(_pos);
        for (int x = Mathf.FloorToInt(_pos.x); x <= Mathf.CeilToInt(_pos.x); x++)
        {
            for (int y = Mathf.FloorToInt(_pos.y); y <= Mathf.CeilToInt(_pos.y); y++)
            {
                result.Add(new Vector2Int(x, y));
            }
        }

        //DebugCustom.Log("Result Count", result.Count);
        return result;
    }

    public static string GetColoredText(string str, Color color)
    {
        return $"<color=#{ColorUtility.ToHtmlStringRGBA(color)}>{str}</color>";
    }
    public static T GetRandomEnum<T>() where T : Enum
    {
        Array values = Enum.GetValues(typeof(T));
        System.Random random = new System.Random();
        T randomBar = (T)values.GetValue(random.Next(values.Length));
        return randomBar;
    }

    public static List<T> GetRandomListEnum<T>(int count) where T : Enum
    {
        List<T> lstT = GetListEnum<T>();
        int lstCount = lstT.Count;
        if (count < lstT.Count)
            for (int i = 0; i < lstCount - count; i++)
            {
                lstT.Remove(lstT[Random.Range(0, lstT.Count)]);
            }

        return lstT;
    }

    public static Dictionary<T, int> GetRandomDicEnum<T>(long count) where T : Enum
    {
        List<T> lstT = GetListEnum<T>();
        int typeCount = lstT.Count;
        Dictionary<T, int> dicValue = new Dictionary<T, int>();
        for (int i = 0; i < typeCount; i++)
        {
            dicValue.Add(lstT[i], 0);
        }

        for (int i = 0; i < count; i++)
        {
            dicValue[lstT[Random.Range(0, typeCount)]]++;
        }

        //DebugCustom.LogColorJson(dicValue);
        Dictionary<T, int> dicResult = new Dictionary<T, int>();
        foreach (var item in dicValue)
        {
            if (item.Value > 0)
                dicResult.Add(item.Key, item.Value);
        }

        //DebugCustom.LogColorJson(dicResult);
        return dicResult;
    }

    public static float GetFloat2(float f)
    {
        return (float)((int)(f * 100f)) / 100f;
    }

    public static float getAngle(Vector3 dir)
    {
        return getAngle(dir.x, dir.y);
    }

    public static float getAngle(float x, float y)
    {
        float getAngle_angle = 90f - Mathf.Atan2(y, x) * Mathf.Rad2Deg;
        getAngle_angle = (getAngle_angle + 360f) % 360f;
        return GetFloat2(getAngle_angle);
    }

    public static float Get360Angle(float angle)
    {
        while (angle < 0f)
        {
            angle += 360f;
        }

        while (angle > 360f)
        {
            angle -= 360f;
        }

        return angle;
    }
    /// <summary>
    /// Get Angle From 0 -> 360
    /// </summary>
    /// <param name="dir"></param>
    /// <returns></returns>
    public static float Get2DAngle(Vector3 dir)
    {
        return Get360Angle(90 - getAngle(dir.x, dir.y));
    }
    public static float Get180Anlge(float angle)
    {
        while (angle > 180)
        {
            angle -= 360;
        }

        while (angle < -180)
        {
            angle += 360;
        }

        return angle;
    }

    public static float Sin(float angle)
    {
        return Mathf.Sin(angle * Mathf.Deg2Rad);
    }

    public static float Cos(float angle)
    {
        return Mathf.Cos(angle * Mathf.Deg2Rad);
    }

    public static Vector2 GetDirectionFromAngle(float angle)
    {
        float horizontal = Cos(angle);
        float vertical = Sin(angle);
        Vector2 dir = new Vector2(horizontal, vertical);
        return dir;
    }

    public static Vector2 GetPositionFromAngle(Vector2 startPos, float angle, float distance)
    {
        float horizontal = Cos(angle);
        float vertical = Sin(angle);
        Vector2 dir = new Vector3(horizontal, vertical);
        Vector2 pos = startPos + dir * distance;
        return pos;
    }
    public static Vector3 GetRandomPosInScreen()
    {
        return new Vector3(
            Random.Range(ResolutionManager.Instance.ScreenLeftEdge, ResolutionManager.Instance.ScreenRightEdge),
            Random.Range(ResolutionManager.Instance.ScreenBottomEdge, ResolutionManager.Instance.ScreenTopEdge));
    }

    public static Vector2 GetPerpendicular2D(Vector2 line)
    {
        // x * line.x + y * line.y = 0;
        if (line.y == 0 && line.x == 0)
            return Vector2.zero;

        float x = 0;
        float y = 0;

        if (line.x != 0)
        {
            y = 1;
            x = -line.y / line.x;
        }

        else if (line.y != 0)
        {
            x = 1;
            y = -line.x / line.y;
        }

        return (new Vector2(x, y)).normalized;
    }

    public static Vector2? IntersectionPoint(Vector2 start1, Vector2 end1, Vector2 start2, Vector2 end2)
    {
        float denominator = (end1.x - start1.x) * (end2.y - start2.y) - (end1.y - start1.y) * (end2.x - start2.x);

        // Check if the two lines are parallel
        if (Mathf.Approximately(denominator, 0f))
        {
            return null; // Lines are parallel and don't intersect
        }

        float t = ((start2.x - start1.x) * (end2.y - start2.y) - (start2.y - start1.y) * (end2.x - start2.x)) /
                  denominator;
        float u = -((start1.x - start2.x) * (end1.y - start1.y) - (start1.y - start2.y) * (end1.x - start1.x)) /
                  denominator;

        if (t >= 0f && t <= 1f && u >= 0f && u <= 1f)
        {
            // Lines intersect at this point
            float intersectionX = start1.x + t * (end1.x - start1.x);
            float intersectionY = start1.y + t * (end1.y - start1.y);
            return new Vector2(intersectionX, intersectionY);
        }
        else
        {
            return null; // Lines don't intersect within their segments
        }
    }

    public static Vector2? IntersectionWithSceneBorder(Vector2 start, Vector2 end, float offset = 0)
    {
        Vector2[] vertices = new Vector2[4];
        vertices[0] = new Vector2(ResolutionManager.Instance.ScreenLeftEdge + offset,
            ResolutionManager.Instance.ScreenTopEdge - offset);
        vertices[1] = new Vector2(ResolutionManager.Instance.ScreenRightEdge - offset,
            ResolutionManager.Instance.ScreenTopEdge - offset);
        vertices[2] = new Vector2(ResolutionManager.Instance.ScreenLeftEdge + offset,
            ResolutionManager.Instance.ScreenBottomEdge + offset);
        vertices[3] = new Vector2(ResolutionManager.Instance.ScreenRightEdge - offset,
            ResolutionManager.Instance.ScreenBottomEdge + offset);

        for (int i = 0; i < 4; i++)
        {
            Vector2 edge = vertices[(i + 1) % 4] - vertices[i];
            Vector2 toStart = start - vertices[i];
            Vector2 toEnd = end - vertices[i];

            float dotStart = Vector2.Dot(toStart, edge);
            float dotEnd = Vector2.Dot(toEnd, edge);

            if (dotStart * dotEnd < 0)
            {
                float t = dotStart / (dotStart - dotEnd);
                Vector2 intersection = start + t * (end - start);
                return intersection;
            }
        }

        return null;
    }

    public static bool AreSegmentsIntersecting(Vector2 start1, Vector2 end1, Vector2 start2, Vector2 end2,
        out Vector2 intersection)
    {
        Vector2 dir1 = end1 - start1;
        Vector2 dir2 = end2 - start2;

        float denom = dir1.x * dir2.y - dir1.y * dir2.x;

        if (Mathf.Approximately(denom, 0f))
        {
            // Lines are parallel, no intersection
            intersection = Vector2.zero;
            return false;
        }

        Vector2 diff = start2 - start1;
        float t = (diff.x * dir2.y - diff.y * dir2.x) / denom;
        float u = (diff.x * dir1.y - diff.y * dir1.x) / denom;

        if (t >= 0f && t <= 1f && u >= 0f && u <= 1f)
        {
            // Lines intersect
            intersection = start1 + t * dir1;
            return true;
        }

        // Lines do not intersect
        intersection = Vector2.zero;
        return false;
    }

    public static bool IsInScene(Vector2 point, float offset = 0)
    {
        Rect rect = new Rect(
            new Vector2(ResolutionManager.Instance.ScreenLeftEdge + offset,
                ResolutionManager.Instance.ScreenBottomEdge + offset),
            new Vector2(ResolutionManager.Instance.WorldWidth - offset * 2,
                ResolutionManager.Instance.WorldHigh - offset * 2));
        return rect.Contains(point);
    }

    public static int GetSortingOder(float transformY)
    {
        return (int)(transformY * -50) + 10000;
    }

    public static List<T> ShufferList<T>(List<T> lstInput)
    {
        int n = lstInput.Count;
        for (int i = 0; i < n; i++)
        {
            int randomIndex = Random.Range(i, n);
            T temp = lstInput[i];
            lstInput[i] = lstInput[randomIndex];
            lstInput[randomIndex] = temp;
        }

        return lstInput;
    }

    public static List<T> GetListEnum<T>()
    {
        Array y = Enum.GetValues(typeof(T));
        List<T> lstEnum = new List<T>();
        foreach (var item in y)
        {
            lstEnum.Add((T)item);
        }

        return lstEnum;
    }

    public static int GetMaxEnum<T>()
    {
        Array y = Enum.GetValues(typeof(T));
        return y.Length;
    }

    public static T ToEnum<T>(this string value)
    {
        return (T)System.Enum.Parse(typeof(T), value, true);
    }

    public static bool TryToEnum<T>(this string value, out T _type) where T : struct
    {
        if (Enum.TryParse<T>(value, out _type))
        {
            return true;
        }

        return false;
    }

    public static string GetI2Translation(string key)
    {
        string message = I2.Loc.LocalizationManager.GetTranslation(key);
        if (string.IsNullOrEmpty(message))
            message = key;
        return message;
    }

    public static string TimeToString(int totalSeconds)
    {
        return TimeToString(System.TimeSpan.FromSeconds(totalSeconds));
    }

    public static string TimeToString(TimeSpan ts)
    {
        if (ts.TotalDays >= 1)
            return ts.ToString(@"dd\:hh\:mm\:ss");
        else if (ts.TotalHours >= 1)
            return ts.ToString(@"hh\:mm\:ss");
        else
            return ts.ToString(@"mm\:ss");
    }

    public static string TimeToShortString(TimeSpan ts)
    {
        if (ts.TotalHours >= 1)
            return ts.ToString(@"hh\:mm");
        else
            return ts.ToString(@"mm\:ss");
    }

    public static DateTime ParseDateTime(long date)
    {
        return DateTimeHelper.ParseUnixTimestampNormal(date);
    }

    public static bool ContainsValidCharacters(string input)
    {
        Regex regex = new Regex("^[a-zA-Z0-9_]+$");
        return regex.IsMatch(input);
    }

    public static float ParseFloat(string value)
    {
        if (value.Contains(","))
            value = value.Replace(",", ".");
        float tmpFloat = float.Parse(value, NumberStyles.Any, CultureInfo.InvariantCulture.NumberFormat);
        return tmpFloat;
    }

    public static int ParseInt(string data)
    {
        int val = 0;
        if (int.TryParse(data, out val))
            return val;
        DebugCustom.LogError("Wrong Input ", data);
        return val;
    }

    public static bool ParseBool(string data)
    {
        bool val = false;
        if (bool.TryParse(data, out val))
            return val;
        DebugCustom.LogError("Wrong Input", data);
        return val;
    }
#if UNITY_EDITOR

    public static void SaveCsvFromString(string csvContent, string destinationFileName)
    {
        string destinationFolder = Application.dataPath + "/Data";
        string destinationFilePath = Path.Combine(destinationFolder, $"{destinationFileName}.csv");

        try
        {
            if (!Directory.Exists(destinationFolder))
            {
                Directory.CreateDirectory(destinationFolder);
            }

            File.WriteAllText(destinationFilePath, csvContent);

            Debug.Log("CSV file saved: " + destinationFilePath);

            AssetDatabase.Refresh();
        }
        catch (IOException ex)
        {
            Debug.LogError("fail to save file CSV: " + ex.Message);
        }
    }

    public static IEnumerator IELoadData(string urlData, System.Action<string> actionComplete,
        string destinationFileName, bool showAlert = false)
    {
        var www = new WWW(urlData);
        float time = 0;
        //TextAsset fileCsvLevel = null;
        while (!www.isDone)
        {
            time += 0.001f;
            if (time > 10000)
            {
                yield return null;
                Debug.Log("Downloading...");
                time = 0;
            }
        }

        if (!string.IsNullOrEmpty(www.error))
        {
            UnityEditor.EditorUtility.DisplayDialog("Notice", "Load CSV Fail", "OK");
            yield break;
        }

        yield return null;
        SaveCsvFromString(www.text, destinationFileName);
        yield return null;
        actionComplete?.Invoke(www.text);
        yield return null;
        UnityEditor.AssetDatabase.SaveAssets();
        if (showAlert)
            UnityEditor.EditorUtility.DisplayDialog("Notice", "Load Data Success", "OK");
        else
            Debug.Log("<color=yellow>Download Data Complete</color>");
    }
#endif
    public static List<T> GetRandomByPercent<T>(Dictionary<T, float> dicPercent, int count, bool repeat = false)
    {
        Dictionary<T, float> dicRate = new Dictionary<T, float>(dicPercent);
        List<T> result = new List<T>();
        if (!repeat && count > dicRate.Count)
        {
            Debug.LogError("Invalid Input");
            return null;
        }

        for (int i = 0; i < count; i++)
        {
            T t = From<T>(dicRate);
            if (!repeat)
                dicRate.Remove(t);
            result.Add(t);
        }

        return result;
    }

    public static T GetRandomByPercent<T>(Dictionary<T, float> dicPercent)
    {
        T result = From<T>(dicPercent);
        return result;
    }

    private static T From<T>(Dictionary<T, float> spawnRate)
    {
        WeightedRandomBag<T> bag = new WeightedRandomBag<T>();
        foreach (var item in spawnRate)
        {
            bag.AddEntry(item.Key, item.Value);
        }

        return bag.GetRandom();
        //return new WeightedRandomizer<T>(spawnRate);
    }
}

public class WeightedRandomBag<T>
{
    private struct Entry
    {
        public float accumulatedWeight;
        public T item;
    }

    private List<Entry> entries = new List<Entry>();
    private float accumulatedWeight;
    private System.Random rand = new System.Random();


    public void AddEntry(T item, float weight)
    {
        this.accumulatedWeight += weight;
        entries.Add(new Entry { item = item, accumulatedWeight = weight });
    }

    public T GetRandom()
    {
        //double r = rand.NextDouble() * accumulatedWeight;

        //foreach (Entry entry in entries)
        //{
        //    if (entry.accumulatedWeight >= r)
        //    {
        //        return entry.item;
        //    }
        //}

        var randomPoint = UnityEngine.Random.value * this.accumulatedWeight;

        for (int i = 0; i < entries.Count; i++)
        {
            if (randomPoint < entries[i].accumulatedWeight)
                return entries[i].item;
            else
                randomPoint -= entries[i].accumulatedWeight;
        }

        return default(T); //should only happen when there are no entries
    }
}