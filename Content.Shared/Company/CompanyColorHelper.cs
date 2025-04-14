using Robust.Shared.Maths;
using System;
using System.Text;

namespace Content.Shared.Company;

/// <summary>
/// Helper class for generating consistent company colors across client and server.
/// </summary>
public static class CompanyColorHelper
{
    /// <summary>
    /// Helper method to color text with markup.
    /// </summary>
    public static string ColorText(string text, Color color)
    {
        return $"[color=#{(byte)(color.R * 255):X2}{(byte)(color.G * 255):X2}{(byte)(color.B * 255):X2}]{text}[/color]";
    }
    
    /// <summary>
    /// Generates a deterministic color based on a deterministic hash of a string.
    /// Ensures reasonable saturation and value for visibility.
    /// </summary>
    public static Color GetDeterministicColor(string name)
    {
        if (string.IsNullOrEmpty(name))
            return Color.White; // Default color for empty names
            
        // Always return white for "None" company
        if (name.Equals("None", StringComparison.OrdinalIgnoreCase))
            return Color.White;

        // Normalize the string to ensure consistent hash codes
        // Use invariant culture and lowercase to avoid culture-specific differences
        string normalizedName = name.ToLowerInvariant();
        
        // Compute a deterministic hash using FNV-1a algorithm
        // This is more consistent across different environments than GetHashCode()
        uint hash = ComputeFnvHash(normalizedName);
        
        // Use hash to generate HSV color values
        // Hue: Use the full spectrum (0-360)
        var hue = (hash % 360) / 360f;
        
        // Fixed saturation and value for good visibility and distinctiveness
        const float saturation = 1.0f;
        const float value = 0.8f;

        return Color.FromHsv(new Vector4(hue, saturation, value, 1.0f));
    }
    
    /// <summary>
    /// Computes a deterministic hash code using the FNV-1a algorithm.
    /// This is more reliable than GetHashCode() for cross-system consistency.
    /// </summary>
    private static uint ComputeFnvHash(string text)
    {
        // FNV-1a hash algorithm constants
        const uint fnvPrime = 16777619;
        const uint fnvOffsetBasis = 2166136261;
        
        uint hash = fnvOffsetBasis;
        
        // Convert string to bytes using UTF-8 encoding for consistency
        byte[] bytes = Encoding.UTF8.GetBytes(text);
        
        // Compute hash
        foreach (byte b in bytes)
        {
            hash ^= b;
            hash *= fnvPrime;
        }
        
        return hash;
    }
} 