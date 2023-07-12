using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Graphics.Shaders;

namespace AcidicBosses.Common.Primitive;

public class PrimDrawer
{
    public struct VertexPosition2DColor : IVertexType
    {
        public Vector2 Position;
        public Color Color;
        public Vector2 TextureCoordinates;
        public VertexDeclaration VertexDeclaration => _vertexDeclaration;

        private static readonly VertexDeclaration _vertexDeclaration = new(new VertexElement[]
        {
            new VertexElement(0, VertexElementFormat.Vector2, VertexElementUsage.Position, 0),
            new VertexElement(8, VertexElementFormat.Color, VertexElementUsage.Color, 0),
            new VertexElement(12, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0),
        });
        public VertexPosition2DColor(Vector2 position, Color color, Vector2 textureCoordinates)
        {
            Position = position;
            Color = color;
            TextureCoordinates = textureCoordinates;
        }
    }
    
    internal Matrix? PerspectiveMatrixOverride;
    
    public BasicEffect BaseEffect;
    public MiscShaderData Shader;

    public PrimDrawer(MiscShaderData shader = null)
    {
        if(shader != null) Shader = shader;
        BaseEffect = new BasicEffect(Main.instance.GraphicsDevice)
        {
            VertexColorEnabled = true,
            TextureEnabled = false
        };
    }
    
    # region Helpers
    
    // Stuff in this section was blatantly stolen from Infernum because I don't feel like reinventing the wheel
    private void UpdateBaseEffect(out Matrix effectProjection, out Matrix effectView)
    {
        // Get the screen bounds.
        int height = Main.instance.GraphicsDevice.Viewport.Height;

        // Get the zoom and the scaling zoom matrix from it.
        Vector2 zoom = Main.GameViewMatrix.Zoom;
        Matrix zoomScaleMatrix = Matrix.CreateScale(zoom.X, zoom.Y, 1f);

        // Get a matrix that aims towards the Z axis (these calculations are relative to a 2D world).
        effectView = Matrix.CreateLookAt(Vector3.Zero, Vector3.UnitZ, Vector3.Up);

        // Offset the matrix to the appropriate position based off the height.
        effectView *= Matrix.CreateTranslation(0f, -height, 0f);

        // Flip the matrix around 180 degrees.
        effectView *= Matrix.CreateRotationZ(MathHelper.Pi);

        // Account for the inverted gravity effect.
        if (Main.LocalPlayer.gravDir == -1f)
            effectView *= Matrix.CreateScale(1f, -1f, 1f) * Matrix.CreateTranslation(0f, height, 0f);

        // And account for the current zoom.
        effectView *= zoomScaleMatrix;

        // Create a projection in 2D using the screen width/height, and the zoom.
        effectProjection = Matrix.CreateOrthographicOffCenter(0f, Main.screenWidth * zoom.X, 0f, Main.screenHeight * zoom.Y, 0f, 1f) * zoomScaleMatrix;
        BaseEffect.View = effectView;
        BaseEffect.Projection = effectProjection;
    }

    private void UpdateBaseEffectPixel(out Matrix effectProjetion, out Matrix effectView)
    {
        effectProjetion = Matrix.CreateOrthographicOffCenter(0, Main.screenWidth, Main.screenHeight, 0, -1, 1);
        effectView = Matrix.Identity;
        BaseEffect.Projection = effectProjetion;
        BaseEffect.View = effectView;
    }
    
    public void DrawPrimsFromVertexData(IEnumerable<VertexPosition2DColor> vertices, IEnumerable<short> triangleIndices, bool pixelated)
    {
        if (triangleIndices.Count() % 6 != 0 || vertices.Count() <= 3)
            return;

        Matrix projection;
        Matrix view;

        if (pixelated)
            UpdateBaseEffectPixel(out projection, out view);
        else
            UpdateBaseEffect(out projection, out view);

        Main.instance.GraphicsDevice.RasterizerState = RasterizerState.CullNone;
        Main.instance.GraphicsDevice.RasterizerState.ScissorTestEnable = true;
        Main.instance.GraphicsDevice.ScissorRectangle = new(0, 0, Main.screenWidth, Main.screenHeight);
        
        if (Shader != null)
        {
            Shader.Shader.Parameters["uWorldViewProjection"].SetValue(PerspectiveMatrixOverride ?? view * projection);
            Shader.Apply();
            PerspectiveMatrixOverride = null;
        }
        else
            BaseEffect.CurrentTechnique.Passes[0].Apply();

        Main.instance.GraphicsDevice.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, vertices.ToArray(), 0, vertices.Count(), triangleIndices.ToArray(), 0, triangleIndices.Count() / 3);
        Main.pixelShader.CurrentTechnique.Passes[0].Apply();
    }
    
    #endregion
}