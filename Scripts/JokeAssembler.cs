using Godot;
using System;
using System.Collections.Generic;
using System.Timers;
using GGJ24.Scripts;
using GGJ24.Scripts.JokeParts;
using Timer = System.Timers.Timer;

class ElementWithTransition
{
	public Node2D Element;
	public Vector2 StartLocation;
	public Vector2 DesiredLocation;

	public ElementWithTransition(Node2D element, Vector2 startLocation, Vector2 desiredLocation)
	{
		Element = element;
		StartLocation = startLocation;
		DesiredLocation = desiredLocation;
	}
}

public class JokeAssembler : Node2D
{
	[Export] private NodePath BasePlacePath;
	private Vector2 BasePlace;
	[Export] private float HorizontalSpace = 100f;
	[Export] private float TransitionTime = 1f;
	[Export] private int MaxSequenceLength = 5;
	[Export] private PackedScene Tip;
	[Export] private float TipOffset = 700f;
	[Export] private float JokeBlockTime = 3f;

	private AnimationPlayer FadeOut;
	private AnimationPlayer Delay;

	private Node2D BackGround;
	private RuleHint MismatchRuleHint;
	private RuleHint SpoilerRuleHint;
	private RuleHint RepeatRuleHint;

	private Joke AssembledJoke;

	private List<ElementWithTransition> Elements;
	private float TransitionProgress = 1f;

	public delegate void onLockChangedDelegate(bool bLocked);

	public event onLockChangedDelegate OnLockChanged;

	public delegate void onJokeFailedDelegate();

	public event onJokeFailedDelegate OnJokeFailedDelegate;

	public static JokeAssembler StaticAssembler;
	
	[Signal] public delegate void JokePartAdded(JokePart jokePart);

	public JokeAssembler()
	{
		StaticAssembler = this;
	}

	public bool AddElement(Node2D Element)
	{
		if (AssembledJoke == null)
		{
			AssembledJoke = new Joke(MaxSequenceLength);
		}
		
		if (AssembledJoke.IsFinished())
		{
			return false;
		}
		
		Transform2D GlobalTransform = Element.GlobalTransform;
		if (Element.GetParent() != null)
		{
			Element.GetParent().RemoveChild(Element);
		}

		JokePart NewJokePart = Element as JokePart;
		if (!AssembledJoke.AddPart(NewJokePart))
		{
			return false;
		}
		
		BackGround.AddChild(Element);
		Element.GlobalTransform = GlobalTransform;
		Elements.Add(new ElementWithTransition(Element, Element.Position, Element.Position));
		UpdateDesiredLocations();
		
		EmitSignal(nameof(JokePartAdded), NewJokePart);

		return true;
	}

	private void UpdateDesiredLocations()
	{
		for (int idx = 0; idx < Elements.Count; idx++)
		{
			ElementWithTransition Focus = Elements[idx];
			Focus.StartLocation = Focus.Element.Position;
			Focus.DesiredLocation = BasePlace + Vector2.Right * HorizontalSpace * ((float)idx - (float)Elements.Count / 2);
		}

		TransitionProgress = 0;
	}
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Elements = new List<ElementWithTransition>();
		BasePlace = GetNode<Node2D>(BasePlacePath).Position;
		BackGround = GetNode<Node2D>("Background");
		MismatchRuleHint = GetNode<RuleHint>("%MismatchRuleHint");
		MismatchRuleHint.JokeBlockTime = JokeBlockTime;
		SpoilerRuleHint = GetNode<RuleHint>("%SpoilerRuleHint");
		SpoilerRuleHint.JokeBlockTime = JokeBlockTime;
		RepeatRuleHint = GetNode<RuleHint>("%RepeatRuleHint");
		RepeatRuleHint.JokeBlockTime = JokeBlockTime;
		FadeOut = GetNode<AnimationPlayer>("%FadeOut");
		Delay = GetNode<AnimationPlayer>("%Delay");
	}

	public override void _Process(float delta)
	{
		if (TransitionProgress >= 1)
		{
			return;
		}

		TransitionProgress += Mathf.Clamp(delta / TransitionTime, 0f, 1f);
		
		for (int idx = 0; idx < Elements.Count; idx++)
		{
			ElementWithTransition Focus = Elements[idx];
			Vector2 NewPos = Focus.StartLocation;
			NewPos = NewPos.LinearInterpolate(Focus.DesiredLocation, Mathf.Clamp(TransitionProgress,0, 1));
			Focus.Element.Position = NewPos;
		}
		
		if (TransitionProgress >= 1)
		{
			AddFinished();
		}
	}

	void AddFinished()
	{
		if (AssembledJoke.IsFinished())
		{
			PushJoke();
		}
	}

	void PushJoke()
	{
		if (OnLockChanged != null) 
			OnLockChanged.Invoke(true);

		AssembledJoke.Score();

		if (OnJokeFailedDelegate != null && AssembledJoke.IsFailed())
			OnJokeFailedDelegate.Invoke();
		
		for (int idx = 0; idx < Elements.Count; idx++)
		{
			JokePartTip tip = Tip.InstanceOrNull<JokePartTip>();
			AddChild(tip);
			tip.Position = Elements[idx].DesiredLocation + Vector2.Up * TipOffset;

			JokePartOperationType partType = AssembledJoke.GetPartType(idx);

			string tipText = "";
			Godot.Color tipColor = Godot.Color.ColorN("Green");

			switch (partType)
			{
				case JokePartOperationType.AddOne:
					if (!AssembledJoke.IsFailed())
						tipText = "+1";
					break;
				case JokePartOperationType.AddTwo:
					if (!AssembledJoke.IsFailed())
						tipText = "+2";
					break;
				case JokePartOperationType.MinusOne:
				{
					tipText = "-1";
					tipColor = Godot.Color.ColorN("Red");
					break;
				}
				case JokePartOperationType.MinusTwo:
				{
					tipText = "-2";
					tipColor = Godot.Color.ColorN("Red");
					break;
				}
				case JokePartOperationType.Double:
					tipText = "x2";
					if (AssembledJoke.IsFailed())
						tipColor = Godot.Color.ColorN("Red");
					break;
				case JokePartOperationType.Opener:
					if (!AssembledJoke.IsFailed())
					{
						if (idx == 0)
						{
							tipText = "+3";
						}
						else
						{
							tipText = "+1";
							tipColor = Godot.Color.ColorN("Yellow");
						}
					}

					break;
				case JokePartOperationType.Robot:
					if (!AssembledJoke.IsFailed())
					{
						tipText = "+4";
					}
					else
					{
						tipText = "-8";
						tipColor = Godot.Color.ColorN("Red");
					}
					break;
				case JokePartOperationType.Human:
					if (!AssembledJoke.IsFailed())
						tipText = "+3";
					break;
				case JokePartOperationType.Punchline:
					tipText = "x2";
					if (AssembledJoke.IsFailed())
						tipColor = Godot.Color.ColorN("Red");
					break;
			}

			tip.SetText(tipText, tipColor, JokeBlockTime);
		}

		var jokeResultBackgroundModulateAlpha = 0.6f;
		var jokeResultBackgroundModulate = new Godot.Color(Colors.LightGreen, jokeResultBackgroundModulateAlpha);
		if (AssembledJoke.IsFailed())
		{
			jokeResultBackgroundModulate = new Godot.Color(Colors.LightCoral, jokeResultBackgroundModulateAlpha);

			switch (AssembledJoke.GetFinishReason())
			{
				case FinishReason.ColorAndShapeMismatch:
					MismatchRuleHint.Trigger();
					break;
				case FinishReason.Repeat:
					RepeatRuleHint.Trigger();
					break;
				case FinishReason.Spoiled:
					SpoilerRuleHint.Trigger();
					break;
			}
		}
		FadeOut.Play("1");
		Delay.Play("1");
	}

	void OnJokePushed()
	{
		AssembledJoke = new Joke(MaxSequenceLength);
		
		if (OnLockChanged != null) 
			OnLockChanged.Invoke(false);
		
		for (int idx = 0; idx < Elements.Count; idx++)
		{
			Elements[idx].Element.QueueFree();
		}
			
		Elements.Clear();

		BackGround.Modulate = Colors.White;
		Modulate = Colors.White;
	}


	private void _on_FadeOut_animation_finished(String anim_name)
	{
		Hall.StaticHall.PushJoke(AssembledJoke);
	}
	private void _on_Delay_animation_finished(String anim_name)
	{
		OnJokePushed();
	}

}
