using DarkRift;
using UnityEngine;

public static class SerializationExtensions { 
    public static void WriteBoolsAsBytes(this DarkRiftWriter writer, bool[] bools) {
        for (int i = 0; i < bools.Length; i += 8) {
            int temp = 0;
            for (int j = 0; j < 8; j++) {
                temp += (bools[i + j] ? 1 : 0) << j;
            }
            writer.Write((byte)temp);
        }
    }

    public static bool[] ReadBytesAsBools(this DarkRiftReader reader, int length) {
        var r = new bool[length];

        for (int i = 0; i < length; i += 8) {
            var temp = reader.ReadByte();
            for (int j = 0; j < 8; j++) {
                r[i + j] = ((temp >> j) & 1) == 1;
            }
        }

        return r;
    }

    public static void WriteQuaternion(this DarkRiftWriter writer, Quaternion value) {
        // x*x+y*y+z*z+w*w = 1 => We don't have to send w.
        writer.Write(value.x);
        writer.Write(value.y);
        writer.Write(value.z);
    }

    public static Quaternion ReadQuaternion(this DarkRiftReader reader) {
        var x = reader.ReadSingle();
        var y = reader.ReadSingle();
        var z = reader.ReadSingle();
        var w = Mathf.Sqrt(1f - (x * x + y * y + z * z));

        return new Quaternion(x, y, z, w);
    }

    public static void WriteRadian(this DarkRiftWriter writer, float angle) {
        var a = (ushort)(angle * 10430);
        writer.Write(a);
    }

    public static float ReadRadian(this DarkRiftReader reader) {
        var a = reader.ReadUInt16();
        return a / 10430f;
    }
}
