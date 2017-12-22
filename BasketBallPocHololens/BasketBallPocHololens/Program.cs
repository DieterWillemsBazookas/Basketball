using System;
using System.Collections;
using System.Collections.Generic;
using Windows.ApplicationModel.Core;
using Urho;
using Urho.Gui;
using Urho.SharpReality;
using Urho.Physics;
using Urho.Resources;
using Urho.Shapes;
using System.Threading;
using Urho.Actions;

namespace BasketBallPocHololens
{
    internal class Program
    {
        [MTAThread]
        static void Main()
        {
            var appViewSource = new UrhoAppViewSource<HelloWorldApplication>(new ApplicationOptions("Data"));
            appViewSource.UrhoAppViewCreated += OnViewCreated;
            CoreApplication.Run(appViewSource);
        }

        static void OnViewCreated(UrhoAppView view)
        {
            view.WindowIsSet += View_WindowIsSet;
        }

        static void View_WindowIsSet(Windows.UI.Core.CoreWindow coreWindow)
        {
            // you can subscribe to CoreWindow events here
        }
    }

    public class HelloWorldApplication : StereoApplication
    {
        Node environmentNode;
        Material spatialMaterial;
        Material bucketMaterial;
        bool surfaceIsValid;
        bool positionIsSelected;
        Node bucketNode;
        Node textNode;
        Node startNode;
        Node playNode;

        const int MaxBalls = 1;
        const int durationGame = 5;
        private int ballsThrown = 0;
        private int hits = 0;
        private int misses = 0;
        readonly Queue<Node> balls = new Queue<Node>();
        readonly Color validPositionColor = Color.Gray;
        readonly Color invalidPositionColor = Color.Red;
        bool playStarted = false;

        public HelloWorldApplication(ApplicationOptions opts) : base(opts) { }

        protected override async void Start()
        {
            base.Start();
            environmentNode = Scene.CreateChild();

            // Allow tap gesture
            EnableGestureTapped = true;

            MakeStartNode();

            // Material for spatial surfaces
            spatialMaterial = new Material();
            spatialMaterial.SetTechnique(0, CoreAssets.Techniques.NoTextureUnlitVCol, 1, 1);

            // make sure 'spatialMapping' capabilaty is enabled in the app manifest.
            var spatialMappingAllowed = await StartSpatialMapping(new Vector3(50, 50, 10), 1200);

            //// Use debug renderer to output physics world debug.
            Scene.GetOrCreateComponent<PhysicsWorld>();

            RegisterVoiceCommands();
        }

        private void RegisterVoiceCommands()
        {
            RegisterCortanaCommands(new Dictionary<string, Action>
                {
					//play animations using Cortana
					{"Restart", Restart},
                    {"C", Restart},
                    {"Play Again", PlayAgain},
                    {"P", PlayAgain},
                    {"help", Help }
                });
        }

        void Restart()
        {
            if (restartBoxNode != null)
            {
                System.Diagnostics.Debug.WriteLine("restartBoxNode");
                //availableNodes = new Node[durationGame];
                PlayAgain();
                collissionSphereHoop.Remove();
                bucketNode.Remove();
                bucketNode = null;
                //collissionSphereHoop.Remove();
                collissionSphereHoop = null;
                //leftDetail.Remove();
                leftDetail = null;
                //rightDetail.Remove();
                rightDetail = null;
                collissionNode.Remove();
                collissionNode = null;
                leftGameDetailScreen.Remove();
                leftGameDetailScreen = null;
                leftHitDetailScreen = null;
                rightGameDetailScreen.Remove();
                rightGameDetailScreen = null;
                righthitDetailScreen = null;
                hitsText = null;
                missesText = null;
                positionIsSelected = false;
                playStarted = false;
                MakeStartNode();
            }
        }

        void PlayAgain()
        {
            if (playAgainBoxNode != null)
            {
                endNode.Remove();
                endNode = null;
                playAgainBoxNode = null;
                playAgainNode = null;
                restartBoxNode = null;
                restartNode = null;
                total = 0;
                hits = 0;
                misses = 0;
                ballsThrown = 0;
                hitsText.Text = "Hits " + hits;
                missesText.Text = misses + " Misses";
                foreach (var ball in availableNodes)
                {
                    ball.GetComponent<StaticModel>().SetMaterial(Material.FromImage("Textures/Color_K04.jpg"));
                }
                //balls.Dequeue().Remove();
                System.Diagnostics.Debug.WriteLine("playAgainBoxNode");
            }
        }

        async void Help()
        {
            await TextToSpeech("Available commands are:");
            foreach (var cortanaCommand in CortanaCommands.Keys)
                await TextToSpeech(cortanaCommand);
        }

        private void MakeStartNode()
        {
            // Create a bucket
            startNode = Scene.CreateChild("playNode");
            startNode.Scale = new Vector3(1f, 0.4f, 0.1f);

            // Create instructions
            playNode = startNode.CreateChild();
            var playText3D = playNode.CreateComponent<Text3D>();
            playText3D.HorizontalAlignment = HorizontalAlignment.Center;
            playText3D.VerticalAlignment = VerticalAlignment.Center;
            playText3D.ViewMask = 0x80000000; //hide from raycasts
            playText3D.Text = "Play";
            playText3D.SetFont(CoreAssets.Fonts.AnonymousPro, 26);
            playText3D.SetColor(Color.Black);
            playNode.Translate(new Vector3(0, 0f, -0.55f));

            // Model and Physics for the bucket
            var startNodeModel = startNode.CreateComponent<StaticModel>();
            startNodeModel.Model = CoreAssets.Models.Box;
            startNode.CreateComponent<RigidBody>();
        }

        void OnCollided(NodeCollisionStartEventArgs args)
        {
            //Nog in te vullen
            var bulletNode = args.OtherNode;
            System.Diagnostics.Debug.WriteLine("collision " + bulletNode.Name);
            if (bulletNode.Name.StartsWith("basketball"))
            {
                //Todo

                if (bulletNode.Position.Y < collissionNode.Position.Y)
                {
                    //misses++;
                }
                else
                {
                    if (ballsThrown <= durationGame)
                    {
                        total = hits + misses;
                        if (total < ballsThrown)
                        {
                            hits++;
                            hitsText.Text = "Hits " + hits;
                        }
                    }
                }
                //thrownballs.Remove(bulletNode);
                System.Diagnostics.Debug.WriteLine("hits " + ballsThrown + " in collide " + hits + " misses " + misses);

            }
        }

        Node collissionSphereHoop;
        Node leftDetail;
        Node rightDetail;
        private void MakeHoop(Vector3 pos)
        {
            // Create a bucket
            bucketNode = Scene.CreateChild();
            bucketNode.Position = pos;
            bucketNode.SetScale(0.015f);

            // Model and Physics for the bucket
            var bucketModel = bucketNode.CreateComponent<StaticModel>();
            bucketMaterial = Material.FromColor(validPositionColor);
            bucketModel.Model = ResourceCache.GetModel("Models/hooppie.mdl");
            //bucketModel.Model = ResourceCache.GetModel("Models/basketballHoop.mdl");
            bucketModel.SetMaterial(bucketMaterial);
            bucketModel.ViewMask = 0x80000000; //hide from raycasts
            var body = bucketNode.CreateComponent<RigidBody>();
            var shape = bucketNode.CreateComponent<CollisionShape>();
            shape.SetTriangleMesh(bucketModel.Model, 0, Vector3.One, Vector3.Zero, Quaternion.Identity);

            collissionSphereHoop = bucketNode.CreateChild();
            collissionSphereHoop.Position = new Vector3(0f, 119, -130f);
            //collissionSphereHoop.Position = new Vector3(-37f, 119, 9);
            collissionSphereHoop.Scale = new Vector3(5f, 1, 5f);
            var collissionModel = collissionSphereHoop.CreateComponent<StaticModel>();
            collissionModel.Model = CoreAssets.Models.Sphere;
            collissionModel.SetMaterial(Material.FromColor(Color.Transparent));
            collissionModel.Enabled = false;

            leftDetail = bucketNode.CreateChild();
            leftDetail.Position = new Vector3(-80f, 119, -100);
            leftDetail.Scale = new Vector3(50f, 60f, 0.1f);
            var leftDetailModel = leftDetail.CreateComponent<StaticModel>();
            leftDetailModel.Model = CoreAssets.Models.Box;
            leftDetailModel.SetMaterial(Material.FromColor(Color.Transparent));
            leftDetail.Enabled = false;

            rightDetail = bucketNode.CreateChild();
            rightDetail.Position = new Vector3(80f, 119, -100);
            rightDetail.Scale = new Vector3(50f, 60f, 0.1f);
            var rightDetailModel = rightDetail.CreateComponent<StaticModel>();
            rightDetailModel.Model = CoreAssets.Models.Box;
            rightDetailModel.SetMaterial(Material.FromColor(Color.Transparent));
            rightDetailModel.Enabled = false;

            // Create instructions
            textNode = bucketNode.CreateChild();
            textNode.SetScale(2f);
            var text3D = textNode.CreateComponent<Text3D>();
            text3D.HorizontalAlignment = HorizontalAlignment.Center;
            text3D.VerticalAlignment = VerticalAlignment.Top;
            text3D.ViewMask = 0x80000000; //hide from raycasts
            text3D.Text = "Place on a horizontal\n  surface and click";
            text3D.SetFont(CoreAssets.Fonts.AnonymousPro, 26);
            text3D.SetColor(Color.White);
            textNode.Translate(new Vector3(0, 3f, -0.2f));
        }

        Node collissionNode;
        private void MakeCollissionHoop()
        {
            collissionNode = Scene.CreateChild();
            collissionNode.Name = "CollissionSphere";
            collissionNode.Scale = collissionSphereHoop.WorldScale;
            collissionNode.Position = collissionSphereHoop.WorldPosition;
            var collissionModel = collissionNode.CreateComponent<StaticModel>();
            collissionModel.Model = CoreAssets.Models.Sphere;
            collissionModel.SetMaterial(Material.FromColor(Color.Transparent));
            //collissionModel.Enabled = false;
            var collissionBody = collissionNode.CreateComponent<RigidBody>();
            collissionBody.Trigger = true;
            var collissionShape = collissionNode.CreateComponent<CollisionShape>();
            collissionShape.SetTriangleMesh(collissionModel.Model, 0, Vector3.One, Vector3.Zero, Quaternion.Identity);
            collissionNode.NodeCollisionStart += OnCollided;
        }

        Node leftGameDetailScreen;
        Node leftHitDetailScreen;
        Node rightGameDetailScreen;
        Node righthitDetailScreen;
        Text3D hitsText;
        Text3D missesText;
        Node[] availableNodes = new Node[durationGame];
        private void MakeGameDetailScreen()
        {
            // Create a bucket
            leftGameDetailScreen = Scene.CreateChild("leftGameDetailScreen");
            leftGameDetailScreen.Scale = leftDetail.WorldScale;
            leftGameDetailScreen.Position = leftDetail.WorldPosition;
            //leftGameDetailScreen.Scale = new Vector3(1f, 0.8f, 0.1f);
            //leftGameDetailScreen.Position = new Vector3(bucketNode.WorldPosition.X - 2.5f, bucketNode.WorldPosition.Y + 2, bucketNode.WorldPosition.Z - 3);
            leftGameDetailScreen.Rotation = new Quaternion(0, -LeftCamera.Node.Rotation.ToEulerAngles().Y, 0);

            // Model and Physics for the bucket
            var leftNodeModel = leftGameDetailScreen.CreateComponent<StaticModel>();
            leftNodeModel.Model = CoreAssets.Models.Box;
            leftNodeModel.SetMaterial(Material.FromColor(Color.Transparent));

            // Create instructions
            leftHitDetailScreen = leftGameDetailScreen.CreateChild();
            leftHitDetailScreen.Position = new Vector3(0, 0.3f, 0);
            hitsText = leftHitDetailScreen.CreateComponent<Text3D>();
            hitsText.HorizontalAlignment = HorizontalAlignment.Center;
            hitsText.VerticalAlignment = VerticalAlignment.Center;
            hitsText.ViewMask = 0x80000000; //hide from raycasts
            hitsText.Text = "Hits " + hits;
            hitsText.SetFont(CoreAssets.Fonts.AnonymousPro, 13);
            hitsText.SetColor(Color.FromHex("#5AC4BE"));
            leftHitDetailScreen.Translate(new Vector3(0, 0f, -0.55f));

            // Create instructions
            var ballTextScreen = leftGameDetailScreen.CreateChild();
            var ballText = ballTextScreen.CreateComponent<Text3D>();
            ballText.HorizontalAlignment = HorizontalAlignment.Center;
            ballText.VerticalAlignment = VerticalAlignment.Center;
            ballText.ViewMask = 0x80000000; //hide from raycasts
            ballText.Text = "Balls";
            ballText.SetFont(CoreAssets.Fonts.AnonymousPro, 13);
            ballText.SetColor(Color.FromHex("#5AC4BE"));
            ballTextScreen.Translate(new Vector3(0, 0f, -0.55f));

            var availableBallsNode = leftGameDetailScreen.CreateChild();
            availableBallsNode.Position = new Vector3(0, -0.3f, -0.55f);
            availableBallsNode.Scale = new Vector3(1f, 0.3f, 0.01f);
            var start = -0.4f;
            for (int i = 0; i < durationGame; i++)
            {
                var availableBall = availableBallsNode.CreateChild();
                availableBall.Position = new Vector3(start, 0, 0);
                availableBall.Scale = new Vector3(0.15f, 0.5f, 1f);
                var availableBallModel = availableBall.CreateComponent<StaticModel>();
                availableBallModel.Model = CoreAssets.Models.Sphere;
                availableBallModel.SetMaterial(Material.FromImage("Textures/Color_K04.jpg"));
                availableNodes[i] = availableBall;
                start += 0.2f;
            }

            // Create a bucket
            rightGameDetailScreen = Scene.CreateChild("rightGameDetailScreen");
            rightGameDetailScreen.Scale = rightDetail.WorldScale;
            rightGameDetailScreen.Position = rightDetail.WorldPosition;
            rightGameDetailScreen.Rotation = new Quaternion(0, -LeftCamera.Node.Rotation.ToEulerAngles().Y, 0);

            // Create instructions
            righthitDetailScreen = rightGameDetailScreen.CreateChild();
            righthitDetailScreen.Position = new Vector3(0, 0.3f, 0);
            missesText = righthitDetailScreen.CreateComponent<Text3D>();
            missesText.HorizontalAlignment = HorizontalAlignment.Center;
            missesText.VerticalAlignment = VerticalAlignment.Center;
            missesText.ViewMask = 0x80000000; //hide from raycasts
            missesText.Text = misses + " Misses";
            missesText.SetFont(CoreAssets.Fonts.AnonymousPro, 13);
            missesText.SetColor(Color.FromHex("#5AC4BE"));
            righthitDetailScreen.Translate(new Vector3(0, 0f, -0.55f));

            // Model and Physics for the bucket
            var rightNodeModel = rightGameDetailScreen.CreateComponent<StaticModel>();
            rightNodeModel.Model = CoreAssets.Models.Box;
            rightNodeModel.SetMaterial(Material.FromColor(Color.Transparent));
        }

        protected override void OnUpdate(float timeStep)
        {



            Ray cameraRay = RightCamera.GetScreenRay(0.5f, 0.5f);
            var result = Scene.GetComponent<Octree>().RaycastSingle(cameraRay, RayQueryLevel.Triangle, 100, DrawableFlags.Geometry, 0x70000000);
            if (result != null)
            {
                if (!playStarted)
                {
                    startNode.LookAt(LeftCamera.Node.WorldPosition, new Vector3(0, 1, 0), TransformSpace.World);
                    //startNode.Rotate(new Quaternion(0, 180, 0), TransformSpace.World);
                    //var angle = Vector3.CalculateAngle(new Vector3(0, 1, 0), result.Value.Normal);
                    //surfaceIsValid = angle < 0.3f; //allow only horizontal surfaces
                    //startNode.Position = new Vector3(result.Value.Position.X, result.Value.Position.Y, 3f) /*result.Value.Position*/;
                    //startNode.SetWorldPosition(new Vector3(0.5f, 0.5f, 3f));
                    //LayoutMode.Horizontal
                    Vector3 pos = RightCamera.ScreenToWorldPoint(new Vector3(0.5f, 0.5f, 3f));
                    startNode.Position = pos;
                    //startNode.Position = new Vector3(LeftCamera.Node.WorldPosition.X, LeftCamera.Node.WorldPosition.Y, LeftCamera.Node.WorldPosition.Z + 3f);
                    startNode.Rotation = new Quaternion(0, -LeftCamera.Node.Rotation.ToEulerAngles().Y, 0);
                }
                else if (!positionIsSelected)
                {
                    textNode.LookAt(LeftCamera.Node.WorldPosition, new Vector3(0, 1, 0), TransformSpace.World);
                    textNode.Rotate(new Quaternion(0, 180, 0), TransformSpace.World);
                    var angle = Vector3.CalculateAngle(new Vector3(0, 1, 0), result.Value.Normal);
                    surfaceIsValid = angle < 0.3f; //allow only horizontal surfaces
                    bucketMaterial.SetShaderParameter("MatDiffColor", surfaceIsValid ? validPositionColor : invalidPositionColor);
                    bucketNode.Position = result.Value.Position;
                    bucketNode.Rotation = new Quaternion(0, -LeftCamera.Node.Rotation.ToEulerAngles().Y, 0);
                }
                else if (endNode != null)
                {
                    endNode.LookAt(LeftCamera.Node.WorldPosition, new Vector3(0, 1, 0), TransformSpace.World);
                    Vector3 pos = RightCamera.ScreenToWorldPoint(new Vector3(0.5f, 0.5f, 3f));
                    endNode.Position = pos;
                    endNode.Rotation = new Quaternion(0, -LeftCamera.Node.Rotation.ToEulerAngles().Y, 0);
                }
            }
            else
            {
                // no spatial surfaces found
                surfaceIsValid = false;
            }
        }

        public override void OnGestureTapped()
        {
            if (positionIsSelected)
                ThrowBall();

            if (!playStarted)
            {
                Ray cameraRay = LeftCamera.GetScreenRay(0.5f, 0.5f);
                var result = Scene.GetComponent<Octree>().RaycastSingle(cameraRay, RayQueryLevel.Triangle, 100, DrawableFlags.Geometry, 0x70000000);
                if (result != null)
                {
                    if (result.Value.Node.Name.StartsWith("playNode"))
                    {
                        startNode.Remove();
                        startNode = null;
                        MakeHoop(result.Value.Position);
                        playStarted = true;
                    }
                }
            }
            else
            {
                if (surfaceIsValid && !positionIsSelected)
                {
                    positionIsSelected = true;
                    textNode.Remove();
                    textNode = null;
                    bucketNode.GetComponent<StaticModel>().ApplyMaterialList("Models/baskethoop.txt");
                    MakeCollissionHoop();
                    MakeGameDetailScreen();
                }
                else
                {
                    Ray cameraRay = LeftCamera.GetScreenRay(0.5f, 0.5f);
                    var result = Scene.GetComponent<Octree>().RaycastSingle(cameraRay, RayQueryLevel.Triangle, 100, DrawableFlags.Geometry, 0x70000000);
                    if (result != null)
                    {
                        if (restartNode != null)
                        {
                            System.Diagnostics.Debug.WriteLine("result.Value.Node.Name " + result.Value.Node.Name);
                            System.Diagnostics.Debug.WriteLine("result.Value.Position " + result.Value.Position);
                            //System.Diagnostics.Debug.WriteLine("restartNode.WorldPosition " + LeftCamera.WorldToScreenPoint(restartNode.WorldPosition));
                        }

                        if (result.Value.Node.Name.StartsWith("restartNode"))
                        {
                            System.Diagnostics.Debug.WriteLine("restartNode");
                        }
                    }
                }
            }

            base.OnGestureTapped();
        }

        Node ballNode;
        //List<Node> thrownballs = new List<Node>();
        private int invokeCount = 0;
        int total = 0;
        void ThrowBall()
        {
            if (ballsThrown <= durationGame)
            {
                if (ballsThrown < durationGame)
                {
                    // Create a ball (will be cloned)
                    ballNode = Scene.CreateChild();
                    ballNode.Name = "basketball";
                    ballNode.Position = RightCamera.Node.Position;
                    ballNode.Rotation = RightCamera.Node.Rotation;
                    ballNode.SetScale(0.15f);

                    var ball = ballNode.CreateComponent<StaticModel>();
                    ball.Model = CoreAssets.Models.Sphere;
                    //ball.Model = ResourceCache.GetModel("Models/basketball.mdl");
                    ball.SetMaterial(ResourceCache.GetMaterial("Materials/basketballMaterail.xml"));
                    ball.ViewMask = 0x80000000; //hide from raycasts

                    var ballRigidBody = ballNode.CreateComponent<RigidBody>();
                    ballRigidBody.Mass = 1f;
                    ballRigidBody.RollingFriction = 0.5f;
                    var ballShape = ballNode.CreateComponent<CollisionShape>();
                    ballShape.SetSphere(1, Vector3.Zero, Quaternion.Identity);
                    //ballNode.NodeCollisionStart += OnCollided;

                    ball.GetComponent<RigidBody>().SetLinearVelocity(RightCamera.Node.Rotation * new Vector3(0f, 0.30f, 1f) * 9 /*velocity*/);
                    //Scene.GetComponent<PhysicsWorld>(false).PhysicsPostStep += PhysiscsPostSteps;
                    //Scene.GetComponent<PhysicsWorld>(false).SubscribeToPhysicsPostStep(async args => {

                    //});

                    availableNodes[ballsThrown].GetComponent<StaticModel>().SetMaterial(Material.FromImage("Textures/used.png"));
                    balls.Enqueue(ballNode);
                }

                ballsThrown++;

                //thrownballs.Add(ballNode);
                if (balls.Count > MaxBalls)
                {
                    total = hits + misses;
                    if (total < ballsThrown - 1)
                    {
                        misses++;
                        missesText.Text = misses + " Misses";
                    }
                    balls.Dequeue().Remove();
                    System.Diagnostics.Debug.WriteLine("hits " + ballsThrown + " in physic " + hits + " misses " + misses);
                }

                if (ballsThrown > durationGame)
                {
                    //// Create an AutoResetEvent to signal the timeout threshold in the
                    //// timer callback has been reached.
                    //var autoEvent = new AutoResetEvent(false);

                    //Timer timer = new Timer(RaiseEndMenu, autoEvent, 2000, 0);

                    //// When autoEvent signals the second time, dispose of the timer.
                    //autoEvent.WaitOne();
                    //timer.Dispose();

                    RaiseEndMenu();
                }
            }
        }

        Node endNode;
        Node playAgainBoxNode;
        Node playAgainNode;
        Node restartBoxNode;
        Node restartNode;
        private void RaiseEndMenu()
        {
            balls.Dequeue().Remove();
            total = hits + misses;
            if (total < ballsThrown)
            {
                misses++;
                missesText.Text = misses + " Misses";
            }
            System.Diagnostics.Debug.WriteLine("hits " + ballsThrown + " in physic " + hits + " misses " + misses);

            // Create a bucket
            //endNode = Scene.CreateChild("endNode");
            endNode = Scene.CreateChild();
            endNode.Scale = new Vector3(1.5f, 0.8f, 0.1f);
            endNode.Rotation = new Quaternion(0, -LeftCamera.Node.Rotation.ToEulerAngles().Y, 0);
            // Model and Physics for the bucket
            var endNodeModel = endNode.CreateComponent<StaticModel>();
            endNodeModel.Model = CoreAssets.Models.Box;
            endNodeModel.SetMaterial(Material.FromColor(Color.Transparent));

            playAgainBoxNode = endNode.CreateChild("playAgainNode");
            playAgainBoxNode.Scale = new Vector3(0.25f, 0.15f, 0.1f);
            playAgainBoxNode.Position = new Vector3(0, -0.4f, -0.5f);
            //restartBoxNode.Rotation = new Quaternion(0, -LeftCamera.Node.Rotation.ToEulerAngles().Y, 0);

            // Model and Physics for the bucket
            var playAgainBoxNodeModel = playAgainBoxNode.CreateComponent<StaticModel>();
            playAgainBoxNodeModel.Model = CoreAssets.Models.Box;
            playAgainBoxNodeModel.SetMaterial(Material.FromImage("Textures/border.png"));
            playAgainBoxNode.CreateComponent<RigidBody>();

            // Create instructions
            playAgainNode = playAgainBoxNode.CreateChild();
            //restartNode.Scale = new Vector3(1f, 0.8f, 0.1f);
            //restartNode.Rotation = new Quaternion(0, -LeftCamera.Node.Rotation.ToEulerAngles().Y, 0);
            var playAgainText3D = playAgainNode.CreateComponent<Text3D>();
            playAgainText3D.HorizontalAlignment = HorizontalAlignment.Center;
            playAgainText3D.VerticalAlignment = VerticalAlignment.Center;
            playAgainText3D.ViewMask = 0x80000000; //hide from raycasts
            playAgainText3D.Text = "PLay again";
            playAgainText3D.SetFont(CoreAssets.Fonts.AnonymousPro, 16);
            playAgainText3D.SetColor(Color.FromHex("#5AC4BE"));
            //endNode.Translate(new Vector3(0, 0f, -0.55f));
            playAgainNode.Translate(new Vector3(0, 0f, -3f));
            //endNode.CreateComponent<RigidBody>();

            restartBoxNode = endNode.CreateChild("restartNode");
            restartBoxNode.Scale = new Vector3(0.15f, 0.15f, 0.1f);
            restartBoxNode.Position = new Vector3(-0.2f, 0.4f, -0.5f);
            //restartBoxNode.Rotation = new Quaternion(0, -LeftCamera.Node.Rotation.ToEulerAngles().Y, 0);

            // Model and Physics for the bucket
            var restartBoxNodeModel = restartBoxNode.CreateComponent<StaticModel>();
            restartBoxNodeModel.Model = CoreAssets.Models.Box;
            restartBoxNodeModel.SetMaterial(Material.FromImage("Textures/border.png"));
            restartBoxNode.CreateComponent<RigidBody>();

            // Create instructions
            restartNode = restartBoxNode.CreateChild("restartNode");
            //restartNode.Scale = new Vector3(1f, 0.8f, 0.1f);
            //restartNode.Rotation = new Quaternion(0, -LeftCamera.Node.Rotation.ToEulerAngles().Y, 0);
            var restartText3D = restartNode.CreateComponent<Text3D>();
            restartText3D.HorizontalAlignment = HorizontalAlignment.Center;
            restartText3D.VerticalAlignment = VerticalAlignment.Center;
            restartText3D.ViewMask = 0x80000000; //hide from raycasts
            restartText3D.Text = "Restart";
            restartText3D.SetFont(CoreAssets.Fonts.AnonymousPro, 16);
            restartText3D.SetColor(Color.FromHex("#5AC4BE"));
            //endNode.Translate(new Vector3(0, 0f, -0.55f));
            restartNode.Translate(new Vector3(0, 0f, -3f));

            var scoreScreen = endNode.CreateChild();
            scoreScreen.Scale = new Vector3(0.4f, 0.4f, 0.1f);
            scoreScreen.Position = new Vector3(0, 0.25f, 0);
            var scoreText = scoreScreen.CreateComponent<Text3D>();
            scoreText.HorizontalAlignment = HorizontalAlignment.Center;
            scoreText.VerticalAlignment = VerticalAlignment.Center;
            scoreText.ViewMask = 0x80000000; //hide from raycasts
            scoreText.Text = "Score";
            scoreText.SetFont(CoreAssets.Fonts.AnonymousPro, 13);
            scoreText.SetColor(Color.FromHex("#5AC4BE"));
            scoreScreen.Translate(new Vector3(0, 0f, -0.55f));

            var hitScoreBox = endNode.CreateChild("hitScoreBox");
            hitScoreBox.Scale = new Vector3(0.07f, 0.15f, 0.1f);
            hitScoreBox.Position = new Vector3(-0.1f, 0f, 0);
            //restartBoxNode.Rotation = new Quaternion(0, -LeftCamera.Node.Rotation.ToEulerAngles().Y, 0);

            // Model and Physics for the bucket
            var hitScroeBoxModel = hitScoreBox.CreateComponent<StaticModel>();
            hitScroeBoxModel.Model = CoreAssets.Models.Box;
            hitScroeBoxModel.SetMaterial(Material.FromImage("Textures/border.png"));
            hitScoreBox.CreateComponent<RigidBody>();

            // Create instructions
            var hitScore = hitScoreBox.CreateChild();
            //restartNode.Scale = new Vector3(1f, 0.8f, 0.1f);
            //restartNode.Rotation = new Quaternion(0, -LeftCamera.Node.Rotation.ToEulerAngles().Y, 0);
            var hitScoreText3D = hitScore.CreateComponent<Text3D>();
            hitScoreText3D.HorizontalAlignment = HorizontalAlignment.Center;
            hitScoreText3D.VerticalAlignment = VerticalAlignment.Center;
            hitScoreText3D.ViewMask = 0x80000000; //hide from raycasts
            hitScoreText3D.Text = "" + hits;
            hitScoreText3D.SetFont(CoreAssets.Fonts.AnonymousPro, 26);
            hitScoreText3D.SetColor(Color.FromHex("#5AC4BE"));
            //endNode.Translate(new Vector3(0, 0f, -0.55f));
            hitScore.Translate(new Vector3(0, 0f, -3f));

            var hitScoreTextScreen = endNode.CreateChild();
            hitScoreTextScreen.Scale = new Vector3(0.4f, 0.4f, 0.1f);
            hitScoreTextScreen.Position = new Vector3(-0.1f, -0.2f, 0);
            var hitScoreText = hitScoreTextScreen.CreateComponent<Text3D>();
            hitScoreText.HorizontalAlignment = HorizontalAlignment.Center;
            hitScoreText.VerticalAlignment = VerticalAlignment.Center;
            hitScoreText.ViewMask = 0x80000000; //hide from raycasts
            hitScoreText.Text = "Hits";
            hitScoreText.SetFont(CoreAssets.Fonts.AnonymousPro, 13);
            hitScoreText.SetColor(Color.FromHex("#5AC4BE"));
            hitScoreTextScreen.Translate(new Vector3(0, 0f, -0.55f));

            var missesScoreBox = endNode.CreateChild("missesScoreBox");
            missesScoreBox.Scale = new Vector3(0.07f, 0.15f, 0.1f);
            missesScoreBox.Position = new Vector3(0.1f, 0f, 0);
            //restartBoxNode.Rotation = new Quaternion(0, -LeftCamera.Node.Rotation.ToEulerAngles().Y, 0);

            // Model and Physics for the bucket
            var missesScoreBoxModel = missesScoreBox.CreateComponent<StaticModel>();
            missesScoreBoxModel.Model = CoreAssets.Models.Box;
            missesScoreBoxModel.SetMaterial(Material.FromImage("Textures/border.png"));
            missesScoreBox.CreateComponent<RigidBody>();

            // Create instructions
            var missesScore = missesScoreBox.CreateChild();
            //restartNode.Scale = new Vector3(1f, 0.8f, 0.1f);
            //restartNode.Rotation = new Quaternion(0, -LeftCamera.Node.Rotation.ToEulerAngles().Y, 0);
            var missesScoreText3D = missesScore.CreateComponent<Text3D>();
            missesScoreText3D.HorizontalAlignment = HorizontalAlignment.Center;
            missesScoreText3D.VerticalAlignment = VerticalAlignment.Center;
            missesScoreText3D.ViewMask = 0x80000000; //hide from raycasts
            missesScoreText3D.Text = "" + misses;
            missesScoreText3D.SetFont(CoreAssets.Fonts.AnonymousPro, 26);
            missesScoreText3D.SetColor(Color.FromHex("#5AC4BE"));
            //endNode.Translate(new Vector3(0, 0f, -0.55f));
            missesScore.Translate(new Vector3(0, 0f, -3f));

            var missesScoreTextScreen = endNode.CreateChild();
            missesScoreTextScreen.Scale = new Vector3(0.4f, 0.4f, 0.1f);
            missesScoreTextScreen.Position = new Vector3(0.1f, -0.2f, 0);
            var missesScoreText = missesScoreTextScreen.CreateComponent<Text3D>();
            missesScoreText.HorizontalAlignment = HorizontalAlignment.Center;
            missesScoreText.VerticalAlignment = VerticalAlignment.Center;
            missesScoreText.ViewMask = 0x80000000; //hide from raycasts
            missesScoreText.Text = "Misses";
            missesScoreText.SetFont(CoreAssets.Fonts.AnonymousPro, 13);
            missesScoreText.SetColor(Color.FromHex("#5AC4BE"));
            missesScoreTextScreen.Translate(new Vector3(0, 0f, -0.55f));
        }

        //private void RaiseEndMenu(object state)
        //{
        //    AutoResetEvent autoEvent = (AutoResetEvent)state;

        //    RaiseEndMenu();

        //    ++invokeCount;

        //    if (invokeCount == 1)
        //    {
        //        // Reset the counter and signal the waiting thread.
        //        invokeCount = 0;
        //        autoEvent.Set();
        //    }
        //}

        public override void OnSurfaceAddedOrUpdated(SpatialMeshInfo surface, Model generatedModel)
        {
            bool isNew = false;
            StaticModel staticModel = null;
            Node node = environmentNode.GetChild(surface.SurfaceId, false);
            if (node != null)
            {
                isNew = false;
                staticModel = node.GetComponent<StaticModel>();
            }
            else
            {
                isNew = true;
                node = environmentNode.CreateChild(surface.SurfaceId);
                staticModel = node.CreateComponent<StaticModel>();
            }

            node.Position = surface.BoundsCenter;
            node.Rotation = surface.BoundsRotation;
            staticModel.Model = generatedModel;

            if (isNew)
            {
                staticModel.SetMaterial(spatialMaterial);
                var rigidBody = node.CreateComponent<RigidBody>();
                rigidBody.RollingFriction = 0.5f;
                rigidBody.Friction = 0.5f;
                var collisionShape = node.CreateComponent<CollisionShape>();
                collisionShape.SetTriangleMesh(generatedModel, 0, Vector3.One, Vector3.Zero, Quaternion.Identity);
            }
            else
            {
                //Update Collision shape
            }
        }
    }
}