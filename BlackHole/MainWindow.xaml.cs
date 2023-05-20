using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace BlackHole
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const double GravitationalConstant = 1;
        private const int NumOfObjects = 70;
        private readonly Random random = new Random();
        private readonly DispatcherTimer timer = new DispatcherTimer();
        private readonly List<SpaceObject> spaceObjects = new List<SpaceObject>();

        public MainWindow()
        {
            InitializeComponent();

            InitializeSpaceObjects();
            InitializeCanvasObjects();

            timer.Tick += GameLoop;
            timer.Interval = TimeSpan.FromMilliseconds(15);
            timer.Start();
        }

        private void InitializeSpaceObjects()
        {
            var canvasWidth = myCanvas.Width;
            var canvasHeight = myCanvas.Height;

            var blackHole = CreateBlackHole(canvasWidth / 2, canvasHeight / 2);
            spaceObjects.Add(blackHole);

            var averageDistance = CalculateAverageDistanceToBlackHole(canvasWidth);

            for (int i = 1; i <= NumOfObjects; i++)
            {
                var spaceObject = CreateSpaceObject(blackHole, canvasWidth, canvasHeight, averageDistance);
                spaceObjects.Add(spaceObject);
            }
        }

        private SpaceObject CreateBlackHole(double xPos, double yPos)
        {
            return new SpaceObject
            {
                Id = 0,
                Mass = 1000000,
                Size = 50,
                Color = new SolidColorBrush(Colors.Black),
                Position = new Point(xPos, yPos),
                Velocity = new Vector(0, 0),
                Acceleration = new Vector(0, 0)
            };
        }

        private SpaceObject CreateSpaceObject(SpaceObject blackHole, double canvasWidth, double canvasHeight, double averageDistance)
        {
            // Define a minimum and maximum distance from the black hole
            double minDistance = canvasWidth * 0.1;  // 10% of canvas width
            double maxDistance = canvasWidth * 0.45;  // 45% of canvas width

            // Generate a random angle and distance
            double angle = random.NextDouble() * 2 * Math.PI;  // Angle in radians
            double distance = minDistance + random.NextDouble() * (maxDistance - minDistance);

            // Convert polar coordinates to Cartesian coordinates
            double xPos = blackHole.Position.X + distance * Math.Cos(angle);
            double yPos = blackHole.Position.Y + distance * Math.Sin(angle);
            var position = new Point(xPos, yPos);

            //var position = new Point(random.Next((int)canvasWidth), random.Next((int)canvasHeight));
            var distanceToBlackHole = SpaceMath.Distance(position, blackHole.Position);

            // Scale the distance based on the average distance to get an average distance of 1.
            var scaledDistanceToBlackHole = distanceToBlackHole / averageDistance;

            // Calculate the velocity magnitude needed for a circular orbit
            var velocityMagnitude = Math.Sqrt(GravitationalConstant * blackHole.Mass / scaledDistanceToBlackHole);

            // Reduce the initial velocity to ensure they don't escape the black hole's gravity
            velocityMagnitude *= 0.04; // reduce to 4% of original.

            // Calculate the initial velocity components to make the object orbit the black hole
            var velocity = SpaceMath.CalculateVelocity(blackHole.Position, position, velocityMagnitude);

            // Add random deviation to velocity to create elliptical orbit
            var angleDeviation = (random.NextDouble() - 0.5) * Math.PI / 2; // random angle between -45 and 45 degrees
            velocity = new Vector(velocity.X * Math.Cos(angleDeviation) - velocity.Y * Math.Sin(angleDeviation),
                                  velocity.X * Math.Sin(angleDeviation) + velocity.Y * Math.Cos(angleDeviation));

            return new SpaceObject
            {
                Id = spaceObjects.Count, // Automatically increment the id based on the number of spaceObjects
                Mass = blackHole.Mass * 3 * Math.Pow(10, -6),
                Size = 7,
                Color = new SolidColorBrush(Colors.White),
                Position = position,
                Velocity = velocity,
                Acceleration = new Vector(0, 0)
            };
        }

        private double CalculateAverageDistanceToBlackHole(double canvasWidth)
        {
            double minDistance = canvasWidth * 0.1;  // 10% of canvas width
            double maxDistance = canvasWidth * 0.45;  // 45% of canvas width

            // The average distance is the midpoint between the minimum and maximum distance
            double averageDistance = (minDistance + maxDistance) / 2.0;

            return averageDistance;
        }

        private void InitializeCanvasObjects()
        {
            foreach (SpaceObject spaceObject in spaceObjects)
            {
                myCanvas.Children.Add(new Ellipse
                {
                    Uid = spaceObject.Id.ToString(),
                    Width = spaceObject.Size,
                    Height = spaceObject.Size,
                    Fill = spaceObject.Color,
                    Stroke = spaceObject.Color
                });
            }
        }

        private void GameLoop(object sender, EventArgs e)
        {
            UpdateSpaceObjectsMass();
            UpdateSpaceObjectPositions();
            MoveSpaceObjects();
        }

        private void UpdateSpaceObjectsMass()
        {
            for (int i = 0; i < spaceObjects.Count; i++)
            {
                for (int j = i + 1; j < spaceObjects.Count; j++)
                {
                    if (i != j && SpaceMath.Distance(spaceObjects[i].Position, spaceObjects[j].Position) <= spaceObjects[i].Size / 2)
                    {
                        if (spaceObjects[i].Mass > spaceObjects[j].Mass)
                        {
                            spaceObjects[i].Mass += spaceObjects[j].Mass;
                            RemoveSpaceObjectFromCanvas(spaceObjects[j].Id);
                            spaceObjects.RemoveAt(j);
                            j--;  // adjust the index after removal
                        }
                        else
                        {
                            spaceObjects[j].Mass += spaceObjects[i].Mass;
                            RemoveSpaceObjectFromCanvas(spaceObjects[i].Id);
                            spaceObjects.RemoveAt(i);
                            i--;  // adjust the index after removal
                            break;  // exit the inner loop as the current object i has been removed
                        }
                    }
                }
            }
        }

        private void RemoveSpaceObjectFromCanvas(int id)
        {
            var ellipseToRemove = myCanvas.Children.OfType<Ellipse>().FirstOrDefault(ellipse => ellipse.Uid == id.ToString());
            myCanvas.Children.Remove(ellipseToRemove);
        }

        private void UpdateSpaceObjectPositions()
        {
            foreach (var ellipse in myCanvas.Children.OfType<Ellipse>())
            {
                var spaceObject = spaceObjects.FirstOrDefault(s => s.Id.ToString() == ellipse.Uid);

                if (spaceObject != null)
                {
                    Canvas.SetLeft(ellipse, spaceObject.Position.X - spaceObject.Size / 2);
                    Canvas.SetTop(ellipse, spaceObject.Position.Y - spaceObject.Size / 2);
                }
            }
        }

        private void MoveSpaceObjects()
        {
            var deltaTime = 0.01;

            // Step 1: Update velocities by half a step
            foreach (var spaceObject in spaceObjects)
            {
                spaceObject.Velocity += spaceObject.Acceleration * deltaTime / 2.0;
            }

            // Step 2: Update positions by a full step
            foreach (var spaceObject in spaceObjects)
            {
                spaceObject.Position += spaceObject.Velocity * deltaTime;
            }

            // Step 3: Compute new accelerations
            foreach (var spaceObject in spaceObjects)
            {
                spaceObject.Acceleration = new Vector(0, 0);
            }

            for (int i = 0; i < spaceObjects.Count; i++)
            {
                for (int j = i + 1; j < spaceObjects.Count; j++)
                {
                    if (i != j)
                    {
                        var acceleration1 = SpaceMath.CalculateAcceleration(spaceObjects[i], spaceObjects[j], GravitationalConstant);
                        var acceleration2 = SpaceMath.CalculateAcceleration(spaceObjects[j], spaceObjects[i], GravitationalConstant);
                        spaceObjects[i].Acceleration += acceleration1;
                        spaceObjects[j].Acceleration += acceleration2;
                    }
                }
            }

            // Step 4: Update velocities by another half step
            foreach (var spaceObject in spaceObjects)
            {
                spaceObject.Velocity += spaceObject.Acceleration * deltaTime / 2.0;
            }
        }
    }

    public static class SpaceMath
    {
        public static double Distance(Point point1, Point point2)
        {
            return (point2 - point1).Length;
        }

        public static Vector CalculateVelocity(Point center, Point position, double magnitude)
        {
            var dx = position.X - center.X;
            var dy = position.Y - center.Y;

            // Create a velocity vector that is perpendicular to the position vector (dx, dy)
            var vx = -dy;
            var vy = dx;

            // Normalize the velocity vector
            var length = Math.Sqrt(vx * vx + vy * vy);
            vx /= length;
            vy /= length;

            // Scale the velocity vector to have the desired magnitude
            vx *= magnitude;
            vy *= magnitude;

            return new Vector(vx, vy);
        }

        public static Vector CalculateAcceleration(SpaceObject source, SpaceObject target, double gravitationalConstant)
        {
            var direction = target.Position - source.Position;
            var distanceSquared = direction.LengthSquared;
            if (distanceSquared == 0) return new Vector(0, 0);  // Prevent division by zero
            direction.Normalize();
            return direction * (gravitationalConstant * target.Mass / distanceSquared);
        }
    }

    public class SpaceObject
    {
        public int Id { get; set; }
        public double Mass { get; set; }
        public double Size { get; set; }
        public SolidColorBrush Color { get; set; }
        public Point Position { get; set; }
        public Vector Velocity { get; set; }
        public Vector Acceleration { get; set; }
    }
}
