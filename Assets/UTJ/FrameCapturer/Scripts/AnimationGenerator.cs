using System.Globalization;
using System.IO;

public static class AnimationGenerator
{
    public static string CreationPath = "./Capture/";
    private static string TemplateFileName => "Assets/Animation_template.txt";
    private static string MaterialUpdateFunctionName = nameof(AnimationMaterialHelper.AnimationStarted);
    private static string TemplateText { get; set; }

    public static void CreateAnimation(string path, string modelName, string animationName, int numFrames,
        float duration, int framerate)
    {
        if (TemplateText == null)
        {
            TemplateText = File.ReadAllText(TemplateFileName);
        }

        string fullAnimationName = $"{modelName}_{animationName}";

        string animationFile = TemplateText.Replace("$ANIMATION_NAME", fullAnimationName);
        animationFile = animationFile.Replace("$NUM_FRAMES", (numFrames).ToString());
        animationFile = animationFile.Replace("$ANIMATION_DURATION", (duration).ToString(CultureInfo.InvariantCulture));
        animationFile = animationFile.Replace("$NUM_FRAMES_PER_SECOND", (framerate).ToString(CultureInfo.InvariantCulture));
        animationFile = animationFile.Replace("$MATERIAL_UPDATE_FUNCTION",
            (MaterialUpdateFunctionName).ToString(CultureInfo.InvariantCulture));
        Directory.CreateDirectory(path);
        File.WriteAllText(Path.Combine(path, animationName + ".asset"), animationFile);
    }

    public static string GetFileName(string model, string animation, int perspective, string extension)
    {
        if (!extension.StartsWith("."))
        {
            extension = $".{extension}";
        }

        return Path.Combine(GetDirectory(model), $"{model}_{animation}_{perspective:0000}{extension}");
    }

    public static string GetDirectory(string model)
    {
        return Path.Combine(CreationPath, model);
    }
}