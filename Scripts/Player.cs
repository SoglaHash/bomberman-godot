using Godot;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

public class Player : KinematicBody2D
{
    // Declare member variables here. Examples:
    // private int a = 2;
    // private string b = "text";
    private int _moveSpeed = 100; 
    private int _moveSpeedLimit = 320;

    private AnimationTree _animTree;
    private AnimationNodeStateMachinePlayback _animStateMachine;
    private Vector2 _movement = new Vector2();
    private Vector2 _direction = new Vector2();

    private const string _BombResource = "res://Nodes/Bomb.tscn";
    private PackedScene _packedSceneBomb;

    private int _amountOfBombs = 0;
    public int bombPowerUp = 0;
    public int flamePowerUp = 0;
    public int speedPowerUp = 0;
    public int speedPowerUpValue = 20;
    public int flamePowerUpValue = 1;
    public int bombPowerUpValue = 1;


    private bool _isInvincible = true;
    private AnimatedSprite _sprite;
    private bool _isFlickerOn = true;

    private Timer _timer_invincibility;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        _animTree = (AnimationTree)GetNode("AnimationTree");
        _animStateMachine = (AnimationNodeStateMachinePlayback)_animTree.Get("parameters/playback");

        _packedSceneBomb = ResourceLoader.Load<PackedScene>(_BombResource);
        GD.Print("Player ready!");
        Timer timer_reload = GetNode<Timer>("Reload");
        timer_reload.Connect("timeout", this, "Reload");
        _timer_invincibility = GetNode<Timer>("Invincibility");
        _timer_invincibility.Connect("timeout", this, "RemoveInvincibility");
        Timer timer_invincibility_flicker = GetNode<Timer>("InvincibilityFlicker");
        timer_invincibility_flicker.Connect("timeout", this, "Flicker");
        _sprite = (AnimatedSprite)this.GetNode("./AnimatedSprite");
    }

    private void Reload(){
        if (_amountOfBombs < ( 1 + (bombPowerUp * bombPowerUpValue))){
            _amountOfBombs++;
        }
    }

    private void Flicker()
    {
        if (_isInvincible)
        {
            if (_isFlickerOn)
            {
                _sprite.Modulate = new Color(1, 1, 1, 0.5f);
                _isFlickerOn = false;
            }
            else
            {
                _sprite.Modulate = new Color(1, 1, 1, 1f);
                _isFlickerOn = true;
            }
        }
        else
        {
            if (!_isFlickerOn)
            {
                _isFlickerOn = true;
                _sprite.Modulate = new Color(1, 1, 1, 1f);
            }
        }
    }

    private void RemoveInvincibility(){
        _isInvincible = false;
    }


    private void CollectPrize(){
    }
    private void HitByFire(){
        _timer_invincibility.Start(5.0f);
        _isInvincible = true;
        _timer_invincibility.Connect("timeout", this, "RemoveInvincibility");
    }

//    public override void _Input(InputEvent @event)
//    {
//        // Mouse in viewport coordinates.
//        if (@event is InputEventMouseButton eventMouseButton)
//        {
//            //            GD.Print("Mouse Click/Unclick at: ", eventMouseButton.Position);
//            //            GD.Print("Viewport Resolution is: ", GetViewportRect().Size);
//            //            GD.Print("MousePositon is: ", GetViewport().GetMousePosition());
//            GD.Print("Direction Mouse: ", Position.DirectionTo(eventMouseButton.Position).Normalized());
//            _animTree.Set("parameters/Idle/blend_position", Position.DirectionTo(eventMouseButton.Position).Normalized());
//            _animTree.Set("parameters/Walk/blend_position", Position.DirectionTo(eventMouseButton.Position).Normalized());
//        }
//        else if (@event is InputEventMouseMotion eventMouseMotion)
//        {
//           // _animTree.Set("parameters/Idle/blend_position", Position.DirectionTo(eventMouseMotion.Position).Normalized());
//            // _animTree.Set("parameters/Walk/blend_position", Position.DirectionTo(eventMouseButton.Position).Normalized());
//            //            GD.Print("Mouse Motion at: ", eventMouseMotion.Position);
//            //           GD.Print("Mouse Motion at RELATIVE: ", eventMouseMotion.Relative);
//        }
//        // Print the size of the viewport.
//    }

    public override void _PhysicsProcess(float delta)
    {
        _movement.x = Input.GetActionStrength("move_right") - Input.GetActionStrength("move_left");
        _movement.y = Input.GetActionStrength("move_down") - Input.GetActionStrength("move_up");
        _direction = _movement.Normalized();
        var possibleCollision = MoveAndCollide(_direction * _moveSpeed * delta);


        // Update animation tree based on direction
        string animStateAnimation = _direction == Vector2.Zero ? "Idle" : "Walk";
        _animStateMachine.Travel(animStateAnimation);
        if (animStateAnimation == "Walk"){
            _animTree.Set("parameters/Idle/blend_position", _direction);
            _animTree.Set("parameters/Walk/blend_position", _direction);
        }

        // Check collisions
        if (possibleCollision != null){
            KinematicCollision2D collision = (KinematicCollision2D)possibleCollision;
            Node collider = (Node)collision.Collider;
            GD.Print("Collides with: ", collider.Name);
            if (collider.Name.StartsWith("@Flame")){
                if (!(_isInvincible)){
                    HitByFire();
                }
            }
            else if (collider.Name.StartsWith("Prize")){
                CollectPrize();
            }
            else if (collider.Name.StartsWith("Powerup")){
                // Check type of powerup
                string typeOfPowerUp = ((Powerup)collider).typeOfPowerUp;
                if (typeOfPowerUp == "Powerup_bomb"){
                    bombPowerUp++;
                }
                else if (typeOfPowerUp == "Powerup_flame"){
                    flamePowerUp++;
                }
                else if (typeOfPowerUp == "Powerup_speed"){
                    speedPowerUp++;
                    _moveSpeed = Math.Min(_moveSpeed + speedPowerUpValue, _moveSpeedLimit);
                }
            }
        }

        // Place bomb if key pressed
        if (Input.IsActionPressed("place_bomb") && _amountOfBombs > 0)
        {
            Bomb newBomb = _packedSceneBomb.Instance() as Bomb;
            newBomb.Init(1 + (flamePowerUp * flamePowerUpValue));
            // Change position to center
            // cell size is 32, subtract module then add half cell size (16)
            Vector2 centeredPosition = new Vector2();
            centeredPosition.x = (float)(Math.Round(Position.x  / 32) * 32);
            centeredPosition.y = (float)(Math.Round(Position.y  / 32) * 32);
            // Check if there already is a Bomb on center position
            List<Bomb> bombs = GetTree().Root.GetChildren().OfType<Bomb>().ToList();
            foreach(Bomb bomb in bombs){
                if (bomb.Position == centeredPosition){
                    return;
                }
            }
            newBomb.Position = centeredPosition;
            GetTree().Root.AddChild(newBomb);
            _amountOfBombs--;
        }

    }
}