using Baker76.Imaging;
using System.Runtime.InteropServices;

namespace FontBMSharp
{
    public class Utility
    {
        public static void RotatePoint(ref float x, ref float y, float rotation, float centerX, float centerY)
        {
            float angle = rotation * MathF.PI / 180;
            float cosAngle = MathF.Cos(angle);
            float sinAngle = MathF.Sin(angle);

            // Translate the point so that the pivot is at the origin
            float translatedX = x - centerX;
            float translatedY = y - centerY;

            // Rotate the translated point
            float rotatedX = translatedX * cosAngle - translatedY * sinAngle;
            float rotatedY = translatedX * sinAngle + translatedY * cosAngle;

            // Translate the rotated point back to its original position
            x = rotatedX + centerX;
            y = rotatedY + centerY;
        }

        public static void SnapToGrid(ref short x, ref short y, int gridSize)
        {
            x = (short)(gridSize * (int)MathF.Round((float)x / gridSize));
            y = (short)(gridSize * (int)MathF.Round((float)y / gridSize));
        }

        public static T ToObject<T>(byte[] data, int offset) where T : struct
        {
            T ret = default;
            int objSize = Marshal.SizeOf(typeof(T));
            nint ptr = Marshal.AllocHGlobal(objSize);
            Marshal.Copy(data, offset, ptr, objSize);
            object? obj = Marshal.PtrToStructure(ptr, typeof(T));
            if (obj != null)
                ret = (T)obj;
            Marshal.FreeHGlobal(ptr);
            return ret;
        }

        public static byte[] ToBytes<T>(T obj) where T : struct
        {
            int size = Marshal.SizeOf(obj);
            byte[] arr = new byte[size];
            nint ptr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(obj, ptr, true);
            Marshal.Copy(ptr, arr, 0, size);
            Marshal.FreeHGlobal(ptr);
            return arr;
        }

        public static string GetFileNameWithoutExtension(string pathOrUrl)
        {
            if (Uri.TryCreate(pathOrUrl, UriKind.Absolute, out Uri uri) &&
                (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps || uri.IsFile))
            {
                return Path.GetFileNameWithoutExtension(uri.LocalPath);
            }
            else
            {
                return Path.GetFileNameWithoutExtension(pathOrUrl);
            }
        }

        public static string GetFileName(string pathOrUrl)
        {
            if (Uri.TryCreate(pathOrUrl, UriKind.Absolute, out Uri uri) &&
                (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps || uri.IsFile))
            {
                return Path.GetFileName(uri.LocalPath);
            }
            else
            {
                return Path.GetFileName(pathOrUrl);
            }
        }
    }
}
