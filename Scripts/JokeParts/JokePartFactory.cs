using Godot;

namespace GGJ24.Scripts.JokeParts
{
	public class JokePartFactory : Node
	{
		[Export] public PackedScene JokePartTemplate;

		// Called when the node enters the scene tree for the first time.
		public override void _Ready()
		{
			AddChild(CreateRandom());
		}

		public JokePart CreateRandom()
		{
			return Create(GlobalEnums.GetRandomColor(), GlobalEnums.GetRandomShape(), JokePartOperation.GetRandomJokePartOperationType());
		}

		public JokePart Create(Color inColor, Shape inShape, JokePartOperationType inOperationType)
		{
			GD.Print("Joke part created!");

			var jokePart = JokePartTemplate.InstanceOrNull<JokePart>();
			if (jokePart == null)
			{
				return null;
			}
			
			jokePart.Setup(inColor, inShape, inOperationType);
			return jokePart;
		}

	}
}
