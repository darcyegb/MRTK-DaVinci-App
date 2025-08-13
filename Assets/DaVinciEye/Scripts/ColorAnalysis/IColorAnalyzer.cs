using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace DaVinciEye.ColorAnalysis
{
    /// <summary>
    /// Interface for color analysis, picking, and matching functionality
    /// </summary>
    public interface IColorAnalyzer
    {
        Color PickColorFromImage(Vector2 imageCoordinate);
        Task<Color> AnalyzePaintColorAsync(Vector3 worldPosition);
        ColorMatchResult CompareColors(Color referenceColor, Color paintColor);
        void SaveColorMatch(ColorMatchData matchData);
        List<ColorMatchData> GetColorHistory();
        
        event Action<ColorMatchResult> OnColorAnalyzed;
        event Action<ColorMatchData> OnColorMatchSaved;
        event Action<Color> OnColorPicked;
    }
}