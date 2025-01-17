using System;
using Unity.Collections;

public class TileTools {
    public static string GenerateId(int xIndex, int yIndex) {
        return xIndex + "-" + yIndex;
    }
}