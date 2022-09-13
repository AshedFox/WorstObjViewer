using System.Globalization;
using System.Numerics;
using Lab1.Lib;

Vector3 ParseToVector3(string s)
{
    var values = s.Split(' ').Take(3).ToArray();
    return new Vector3
    {
        X = float.Parse(values[0], NumberStyles.Any, CultureInfo.InvariantCulture),
        Y = float.Parse(values[1], NumberStyles.Any, CultureInfo.InvariantCulture),
        Z = float.Parse(values[2], NumberStyles.Any, CultureInfo.InvariantCulture)
    };
}

//path = @"D:\Загрузки\models\max-model\max.obj";
//path = @"D:\Загрузки\models\shroom-model\Shroom.obj";

Console.WriteLine("Enter .obj file path");
if (Console.ReadLine() is { } path && File.Exists(path) && new FileInfo(path) is { Extension: ".obj" } fileInfo)
{
    Console.Clear();

    Console.WriteLine("Parsing .obj file...");
    ParsedObj result = ObjParser.ParseFromObjFile(File.ReadAllLines(path));
    Console.Clear();

    // TRANSFORMATIONS //
    Console.WriteLine("Transformations:");
    Console.WriteLine("1. Enter rotation vector");
    Console.WriteLine("2. Enter scale vector");

    if (Console.ReadLine() is not { } rotation || Console.ReadLine() is not { } scale)
    {
        Console.WriteLine("incorrect input");
        return;
    }

    var rotationVector = ParseToVector3(rotation);
    var scaleVector = ParseToVector3(scale);
    result.TransformVertices(Matrix4x4.CreateScale(scaleVector));
    result.TransformVertices(Matrix4x4.CreateRotationX(GraphicsProcessor.ConvertDegreesToRadians(rotationVector.X)));
    result.TransformVertices(Matrix4x4.CreateRotationY(GraphicsProcessor.ConvertDegreesToRadians(rotationVector.Y)));
    result.TransformVertices(Matrix4x4.CreateRotationZ(GraphicsProcessor.ConvertDegreesToRadians(rotationVector.Z)));

    Console.Clear();

    // MODEL MATRIX CREATION //
    Console.WriteLine("Model matrix:");
    Console.WriteLine("1. Enter position vector");
    Console.WriteLine("2. Enter forward vector");
    Console.WriteLine("3. Enter up vector");

    if (Console.ReadLine() is not { } mpv || Console.ReadLine() is not { } mfv || Console.ReadLine() is not { } muv)
    {
        Console.WriteLine("incorrect input");
        return;
    }

    Matrix4x4 model = GraphicsProcessor.CreateModelMatrix(
        ParseToVector3(mpv),
        ParseToVector3(mfv),
        ParseToVector3(muv)
    );

    Console.Clear();

    // VIEW MATRIX CREATION //
    Console.WriteLine("View matrix:");
    Console.WriteLine("1. Enter camera position vector");
    Console.WriteLine("2. Enter camera target vector");
    Console.WriteLine("3. Enter camera up vector");

    if (Console.ReadLine() is not { } vpv || Console.ReadLine() is not { } vfv || Console.ReadLine() is not { } vuv)
    {
        Console.WriteLine("Incorrect input");
        return;
    }

    Matrix4x4 view = GraphicsProcessor.CreateViewMatrix(
        ParseToVector3(vpv),
        ParseToVector3(vfv),
        ParseToVector3(vuv)
    );

    Console.Clear();

    // PROJECTION MATRIX CREATION //
    Console.WriteLine("Projection matrix:");
    Console.WriteLine("Select projection type");
    Console.WriteLine("1. Orthographic");
    Console.WriteLine("2. Perspective");
    Console.WriteLine("3. Perspective FOV");

    if (Console.ReadLine() is not { } projectionType)
    {
        Console.WriteLine("Incorrect input");
        return;
    }

    Console.Clear();

    Matrix4x4 projection;

    switch (projectionType)
    {
        case "1":
            Console.WriteLine("Orthographic projection matrix:");
            Console.WriteLine("1. Enter width");
            Console.WriteLine("2. Enter height");
            Console.WriteLine("3. Enter zNear");
            Console.WriteLine("4. Enter zFar");

            if (Console.ReadLine() is not { } oWidth || Console.ReadLine() is not { } oHeight ||
                Console.ReadLine() is not { } oZNear || Console.ReadLine() is not { } oZFar)
            {
                Console.WriteLine("Incorrect input");
                return;
            }

            projection = GraphicsProcessor.CreateOrthographicMatrix(
                float.Parse(oWidth, NumberStyles.Any, CultureInfo.InvariantCulture),
                float.Parse(oHeight, NumberStyles.Any, CultureInfo.InvariantCulture),
                float.Parse(oZNear, NumberStyles.Any, CultureInfo.InvariantCulture),
                float.Parse(oZFar, NumberStyles.Any, CultureInfo.InvariantCulture)
            );
            break;
        case "2":
            Console.WriteLine("Perspective projection matrix:");
            Console.WriteLine("1. Enter width");
            Console.WriteLine("2. Enter height");
            Console.WriteLine("3. Enter zNear");
            Console.WriteLine("4. Enter zFar");

            if (Console.ReadLine() is not { } pWidth || Console.ReadLine() is not { } pHeight ||
                Console.ReadLine() is not { } pZNear || Console.ReadLine() is not { } pZFar)
            {
                Console.WriteLine("Incorrect input");
                return;
            }

            projection = GraphicsProcessor.CreatePerspectiveMatrix(
                float.Parse(pWidth, NumberStyles.Any, CultureInfo.InvariantCulture),
                float.Parse(pHeight, NumberStyles.Any, CultureInfo.InvariantCulture),
                float.Parse(pZNear, NumberStyles.Any, CultureInfo.InvariantCulture),
                float.Parse(pZFar, NumberStyles.Any, CultureInfo.InvariantCulture)
            );
            break;
        case "3":
            Console.WriteLine("Perspective FOV projection matrix:");
            Console.WriteLine("1. Enter width aspect");
            Console.WriteLine("2. Enter height aspect");
            Console.WriteLine("3. Enter fov");
            Console.WriteLine("4. Enter zNear");
            Console.WriteLine("5. Enter zFar");

            if (Console.ReadLine() is not { } widthAspect || Console.ReadLine() is not { } heightAspect ||
                Console.ReadLine() is not { } fov || Console.ReadLine() is not { } zNear ||
                Console.ReadLine() is not { } zFar)
            {
                Console.WriteLine("Incorrect input");
                return;
            }

            projection = GraphicsProcessor.CreatePerspectiveFieldOfViewMatrix(
                float.Parse(widthAspect, NumberStyles.Any, CultureInfo.InvariantCulture) /
                float.Parse(heightAspect, NumberStyles.Any, CultureInfo.InvariantCulture),
                GraphicsProcessor.ConvertDegreesToRadians(
                    float.Parse(fov, NumberStyles.Any, CultureInfo.InvariantCulture)
                ),
                float.Parse(zNear, NumberStyles.Any, CultureInfo.InvariantCulture),
                float.Parse(zFar, NumberStyles.Any, CultureInfo.InvariantCulture)
            );
            break;
        default:
            Console.WriteLine("Incorrect input");
            return;
    }

    Console.Clear();

    // VIEWPORT MATRIX CREATION //
    Console.WriteLine("Viewport matrix:");
    Console.WriteLine("1. Enter width");
    Console.WriteLine("2. Enter height");
    Console.WriteLine("3. Enter xMin");
    Console.WriteLine("4. Enter yMin");

    if (Console.ReadLine() is not { } width || Console.ReadLine() is not { } height ||
        Console.ReadLine() is not { } xMin || Console.ReadLine() is not { } yMin)
    {
        Console.WriteLine("Incorrect input");
        return;
    }

    Matrix4x4 viewport = GraphicsProcessor.CreateViewportMatrix(
        float.Parse(width, NumberStyles.Any, CultureInfo.InvariantCulture),
        float.Parse(height, NumberStyles.Any, CultureInfo.InvariantCulture),
        float.Parse(xMin, NumberStyles.Any, CultureInfo.InvariantCulture),
        float.Parse(yMin, NumberStyles.Any, CultureInfo.InvariantCulture)
    );

    Console.Clear();

    Console.WriteLine("Transforming...");
    result.TransformVertices(viewport * projection * view * model);
    Console.Clear();

    /*result.TransformVertices(Matrix4x4.CreateScale(new Vector3(1.0f, 2.0f, 1.0f)));
    //result.TransformVertices(Matrix4x4.CreateRotationX(GraphicsProcessor.ConvertDegreesToRadians(45)));
    result.TransformVertices(Matrix4x4.CreateRotationY(GraphicsProcessor.ConvertDegreesToRadians(45)));
    //result.TransformVertices(Matrix4x4.CreateRotationZ(GraphicsProcessor.ConvertDegreesToRadians(45)));

    Matrix4x4 model = GraphicsProcessor.CreateModelMatrix(
        new Vector3(1.0f, 1.0f, 1.0f),
        new Vector3(1.0f, 0, 0.0f),
        new Vector3(0, 1.0f, 0)
    );
    Matrix4x4 view = GraphicsProcessor.CreateViewMatrix(
        new Vector3(0.0f, 0.0f, 0.0f),
        new Vector3(1.0f, 0.0f, 0.0f),
        new Vector3(0.0f, 1.0f, 0.0f)
    );
    Matrix4x4 projectionFov = GraphicsProcessor.CreatePerspectiveFieldOfViewMatrix(
        16.0f / 9.0f,
        GraphicsProcessor.ConvertDegreesToRadians(45.0f),
        1.0f,
        100.0f
    );
    Matrix4x4 projectionP = GraphicsProcessor.CreatePerspectiveMatrix(
        80.0f,
        80.0f,
        1.0f,
        100.0f
    );
    Matrix4x4 projectionO = GraphicsProcessor.CreateOrthographicMatrix(
        640.0f,
        480.0f,
        0.1f,
        100.0f
    );
    Matrix4x4 viewport = GraphicsProcessor.CreateViewportMatrix(1920.0f, 1080.0f, 0.0f, 0.0f);

    result.TransformVertices(viewport * projectionFov * view * model);*/

    Console.WriteLine("Saving...");

    File.WriteAllText(
        $"{fileInfo.DirectoryName}/{Path.GetFileNameWithoutExtension(path)}-changed-model{fileInfo.Extension}",
        result.ToString()
    );

    File.WriteAllText(
        $"{fileInfo.DirectoryName}/{Path.GetFileNameWithoutExtension(path)}-changed-wire{fileInfo.Extension}",
        ObjParser.ParseToWired(result)
    );

    Console.Clear();
    Console.WriteLine("Successful");
}
else
{
    Console.WriteLine("Incorrect file");
}
