﻿using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Graphics.Shaders;

namespace AcidicBosses.Common.Primitive;

public class TrailPrimDrawer : PrimDrawer
{
    public delegate float VertexWidthFunction(float completionRatio);

    public delegate Vector2 VertexOffsetFunction(float completionRatio);

    public delegate Color VertexColorFunction(float completionRatio);

    public VertexWidthFunction WidthFunction;

    public VertexColorFunction ColorFunction;

    public VertexOffsetFunction OffsetFunction;

    public bool UsesSmoothening;

    public TrailPrimDrawer(VertexWidthFunction widthFunction, VertexColorFunction colorFunction,
        VertexOffsetFunction offsetFunction = null, bool useSmoothening = true, MiscShaderData specialShader = null) :
        base(specialShader)
    {
        if (widthFunction is null || colorFunction is null)
            throw new NullReferenceException(
                $"In order to create a primitive trail, a non-null {(widthFunction is null ? "width" : "color")} function must be specified.");
        WidthFunction = widthFunction;
        ColorFunction = colorFunction;
        OffsetFunction = offsetFunction;

        UsesSmoothening = useSmoothening;
    }
    
    public List<Vector2> GetTrailPoints(IEnumerable<Vector2> originalPositions, Vector2 generalOffset, int totalTrailPoints)
        {
            // Don't smoothen the points unless explicitly told do so.
            if (!UsesSmoothening)
            {
                List<Vector2> basePoints = originalPositions.Where(originalPosition => originalPosition != Vector2.Zero).ToList();
                List<Vector2> endPoints = new();

                if (basePoints.Count <= 2)
                    return endPoints;

                // Remap the original positions across a certain length.
                for (int i = 0; i < basePoints.Count; i++)
                {
                    Vector2 offset = generalOffset;
                    if (OffsetFunction != null)
                        offset += OffsetFunction(i / (float)(basePoints.Count - 1f));

                    endPoints.Add(basePoints[i] + offset);
                }
                return endPoints;
            }

            List<Vector2> controlPoints = new();
            for (int i = 0; i < originalPositions.Count(); i++)
            {
                // Don't incorporate points that are zeroed out.
                // They are almost certainly a result of incomplete oldPos arrays.
                if (originalPositions.ElementAt(i) == Vector2.Zero)
                    continue;

                float completionRatio = i / (float)originalPositions.Count();
                Vector2 offset = generalOffset;
                if (OffsetFunction != null)
                    offset += OffsetFunction(completionRatio);
                controlPoints.Add(originalPositions.ElementAt(i) + offset);
            }
            List<Vector2> points = new();

            // Avoid stupid index errors.
            if (controlPoints.Count <= 4)
                return controlPoints;

            for (int j = 0; j < totalTrailPoints; j++)
            {
                float splineInterpolant = j / (float)totalTrailPoints;
                float localSplineInterpolant = splineInterpolant * (controlPoints.Count - 1f) % 1f;
                int localSplineIndex = (int)(splineInterpolant * (controlPoints.Count - 1f));

                Vector2 farLeft;
                Vector2 left = controlPoints[localSplineIndex];
                Vector2 right = controlPoints[localSplineIndex + 1];
                Vector2 farRight;

                // Special case: If the spline attempts to access the previous/next index but the index is already at the very beginning/end, simply
                // cheat a little bit by creating a phantom point that's mirrored from the previous one.
                if (localSplineIndex <= 0)
                {
                    Vector2 mirrored = left * 2f - right;
                    farLeft = mirrored;
                }
                else
                    farLeft = controlPoints[localSplineIndex - 1];

                if (localSplineIndex >= controlPoints.Count - 2)
                {
                    Vector2 mirrored = right * 2f - left;
                    farRight = mirrored;
                }
                else
                    farRight = controlPoints[localSplineIndex + 2];

                points.Add(Vector2.CatmullRom(farLeft, left, right, farRight, localSplineInterpolant));
            }

            // Manually insert the front and end points.
            points.Insert(0, controlPoints.First());
            points.Add(controlPoints.Last());

            return points;
        }

        public List<VertexPosition2DColor> GetVerticesFromTrailPoints(List<Vector2> trailPoints, float? directionOverride = null)
        {
            List<VertexPosition2DColor> vertices = new();

            for (int i = 0; i < trailPoints.Count - 1; i++)
            {
                float completionRatio = i / (float)trailPoints.Count;
                float widthAtVertex = WidthFunction(completionRatio);
                Color vertexColor = ColorFunction(completionRatio);

                Vector2 currentPosition = trailPoints[i];
                Vector2 positionAhead = trailPoints[i + 1];
                Vector2 directionToAhead = (positionAhead - trailPoints[i]).SafeNormalize(Vector2.Zero);
                if (directionOverride.HasValue)
                    directionToAhead = directionOverride.Value.ToRotationVector2();

                Vector2 leftCurrentTextureCoord = new(completionRatio, 0f);
                Vector2 rightCurrentTextureCoord = new(completionRatio, 1f);

                // Point 90 degrees away from the direction towards the next point, and use it to mark the edges of the rectangle.
                // This doesn't use RotatedBy for the sake of performance (there can potentially be a lot of trail points).
                Vector2 sideDirection = new(-directionToAhead.Y, directionToAhead.X);

                // What this is doing, at its core, is defining a rectangle based on two triangles.
                // These triangles are defined based on the width of the strip at that point.
                // The resulting rectangles combined are what make the trail itself.
                vertices.Add(new VertexPosition2DColor(currentPosition - sideDirection * widthAtVertex, vertexColor, leftCurrentTextureCoord));
                vertices.Add(new VertexPosition2DColor(currentPosition + sideDirection * widthAtVertex, vertexColor, rightCurrentTextureCoord));
            }

            return vertices;
        }

        public static List<short> GetIndicesFromTrailPoints(int pointCount)
        {
            // What this is doing is basically representing each point on the vertices list as
            // indices. These indices should come together to create a tiny rectangle that acts
            // as a segment on the trail. This is achieved here by splitting the indices (or rather, points)
            // into 2 triangles, which requires 6 points.
            // The logic here basically determines which indices are connected together.
            int totalIndices = (pointCount - 1) * 6;
            short[] indices = new short[totalIndices];

            for (int i = 0; i < pointCount - 2; i++)
            {
                int startingTriangleIndex = i * 6;
                int connectToIndex = i * 2;
                indices[startingTriangleIndex] = (short)connectToIndex;
                indices[startingTriangleIndex + 1] = (short)(connectToIndex + 1);
                indices[startingTriangleIndex + 2] = (short)(connectToIndex + 2);
                indices[startingTriangleIndex + 3] = (short)(connectToIndex + 2);
                indices[startingTriangleIndex + 4] = (short)(connectToIndex + 1);
                indices[startingTriangleIndex + 5] = (short)(connectToIndex + 3);
            }

            return indices.ToList();
        }

        public void SpecifyPerspectiveMatrix(Matrix m) => PerspectiveMatrixOverride = m;

        /// <summary>
        /// Call this to draw primitives to the base RenderTarget.
        /// </summary>
        /// <param name="originalPositions"></param>
        /// <param name="generalOffset"></param>
        /// <param name="totalTrailPoints"></param>
        /// <param name="directionOverride"></param>
        public void Draw(IEnumerable<Vector2> originalPositions, Vector2 generalOffset, int totalTrailPoints, float? directionOverride = null) => DrawPrims(originalPositions, generalOffset, totalTrailPoints, false, directionOverride);

        /// <summary>
        /// Call this to draw primitives to the pixelated RenderTarget. Should only be called in <see cref="System.Drawing.Graphics.IPixelPrimitiveDrawer.DrawPixelPrimitives(SpriteBatch)"/>
        /// </summary>
        /// <param name="originalPositions"></param>
        /// <param name="generalOffset"></param>
        /// <param name="totalTrailPoints"></param>
        /// <param name="directionOverride"></param>
        public void DrawPixelated(IEnumerable<Vector2> originalPositions, Vector2 generalOffset, int totalTrailPoints, float? directionOverride = null) => DrawPrims(originalPositions, generalOffset, totalTrailPoints, true, directionOverride);

        private void DrawPrims(IEnumerable<Vector2> originalPositions, Vector2 generalOffset, int totalTrailPoints, bool pixelated, float? directionOverride = null)
        {
            if (originalPositions.Count() <= 2)
                return;

            originalPositions = originalPositions.Where(p => p != Vector2.Zero);
            List<Vector2> trailPoints = GetTrailPoints(originalPositions, generalOffset, totalTrailPoints);

            // A trail with only one point or less has nothing to connect to, and therefore, can't make a trail.
            if (trailPoints.Count <= 2)
                return;

            // If the trail point has any NaN positions, don't draw anything.
            if (trailPoints.Any(point => point.HasNaNs()))
                return;

            // If the trail points are all equal, don't draw anything.
            if (trailPoints.All(point => point == trailPoints[0]))
                return;

            DrawPrimsFromVertexData(GetVerticesFromTrailPoints(trailPoints, directionOverride), GetIndicesFromTrailPoints(trailPoints.Count), pixelated);
        }
}