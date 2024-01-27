using GGJ24.Scripts.Shapes;
using Godot;

namespace GGJ24.Scripts.JokeParts
{
	public class JokePart : Node2D
	{
		public Color Color;
		public Shape Shape;
		public JokePartOperationType OperationType;
		
		public JokePartOperation Operation;

		public void Setup(Color inColor, Shape inShape, JokePartOperationType inOperationType)
		{
			Color = inColor;
			Shape = inShape;
			OperationType = inOperationType;
		}

		// Called when the node enters the scene tree for the first time.
		public override void _Ready()
		{
			var jokePartOperationFactory = GetNode<JokePartOperationFactory>("%JokePartOperationFactory");
			if (jokePartOperationFactory != null)
			{
				Operation = jokePartOperationFactory.Create(OperationType);
				AddChild(Operation);
			}

			var shapesStorage = GetNode<ShapesStorage>("%ShapesStorage");
			var shapeSprite = GetNode<Sprite>("%ShapeSprite");
			if (shapesStorage != null && shapeSprite != null)
			{
				var shapeData = shapesStorage.GetShape(Shape);
				shapeSprite.Texture = shapeData.Texture;
				shapeSprite.Scale = new Vector2(2, 2);
			}
		}

	}
}