using UnityEngine;
namespace GrygTools.Audio
{
	internal static class InternalUtils
	{
		public static Texture2D GetDominantTerrainTexture(Vector3 pos, Terrain terrain)
		{
			if (terrain == null) return null;

			TerrainData tData = terrain.terrainData;
			Vector3 terrainPos = terrain.transform.position;

			float localX = pos.x - terrainPos.x;
			float localZ = pos.z - terrainPos.z;

			int mapX = Mathf.FloorToInt((localX / tData.size.x) * tData.alphamapWidth);
			int mapZ = Mathf.FloorToInt((localZ / tData.size.z) * tData.alphamapHeight);

			float[,,] alphaMaps = tData.GetAlphamaps(mapX, mapZ, 1, 1);

			int dominantLayer = 0;
			float maxWeight = 0f;

			for (int i = 0; i < alphaMaps.GetLength(2); i++)
			{
				if (alphaMaps[0, 0, i] > maxWeight)
				{
					maxWeight = alphaMaps[0, 0, i];
					dominantLayer = i;
				}
			}
			
			if (dominantLayer == -1) return null;

			if (tData.terrainLayers != null && dominantLayer < tData.terrainLayers.Length)
			{
				TerrainLayer currentLayer = tData.terrainLayers[dominantLayer];
        
				if (currentLayer != null)
				{
					return currentLayer.diffuseTexture;
				}
			}

			return null;
		}
	}
}
