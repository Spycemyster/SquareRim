using Godot;
using System;

public class PlayerHUD : Control
{
	public string InteractText
	{
		get { return interactLabel_.Text; }
		set { interactLabel_.Text = value; }
	}
	[Export]
	public NodePath Player
	{
		get 
		{
			return mPlayerPath;
		}
		set 
		{
			mPlayerPath = value;
			if (IsInsideTree())
			{
				mPlayer = GetNode<Player>(mPlayerPath);
			}
		}
	}
	private NodePath mPlayerPath;

	private Player mPlayer;
	private ColorRect mCurrentHealthRect, mCurrentExpRect;
	private Label mPowerLevelLabel, interactLabel_;
	public override void _Ready()
	{
		mCurrentHealthRect = GetNode<ColorRect>("Health/CurrentHealth");
		mCurrentExpRect = GetNode<ColorRect>("Experience/CurrentExp");
		mPowerLevelLabel = GetNode<Label>("PowerLevelLb");
		mPlayer = GetNode<Player>(mPlayerPath);
		interactLabel_ = GetNode<Label>("InteractLabel");
	}

	public override void _Process(float delta)
	{
		mCurrentHealthRect.RectScale = new Vector2(mPlayer.CurrentHealth / mPlayer.MaxHealth, 1.0f);
		mPowerLevelLabel.Text = $"PR {mPlayer.PowerLevel}";
	}
}
