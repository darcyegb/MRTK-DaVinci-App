using System;
using System.Collections.Generic;
using UnityEngine;

namespace DaVinciEye.Filters
{
    /// <summary>
    /// Interface for real-time filter processing and management
    /// </summary>
    public interface IFilterProcessor
    {
        Texture2D ProcessedTexture { get; }
        List<FilterData> ActiveFilters { get; }
        
        void ApplyFilter(FilterType filterType, FilterParameters parameters);
        void UpdateFilterParameters(FilterType filterType, FilterParameters parameters);
        void RemoveFilter(FilterType filterType);
        void ClearAllFilters();
        
        event Action<Texture2D> OnFilterApplied;
        event Action<FilterType> OnFilterRemoved;
        event Action OnFiltersCleared;
    }
}