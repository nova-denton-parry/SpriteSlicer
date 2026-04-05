using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

Console.WriteLine("\n             Welcome to Sprite Slicer!");
Console.WriteLine("===================================================");
Console.WriteLine("** Enter 'cancel' to stop or restart the program **");


do
{
    try
    {
        string? spriteSheetPath;

        // validate file path
        do
        {
            Console.Write("\nEnter the path to your sprite sheet: ");
            spriteSheetPath = AssignInputOrCancel().Trim('"');

            if (!File.Exists(spriteSheetPath))
                Console.WriteLine("File not found. Please check the path and try again.");
        } while (!File.Exists(spriteSheetPath));

        // load in the image and get the height (referenced later)
        using Image<Rgba32> image = Image.Load<Rgba32>(spriteSheetPath);
        int totalHeight = image.Height;

        string outputFolder = GetOrCreateFolder("output");

        int stageWidth = ValidatePositiveInt(
            "Enter the width of your individual stages in pixels: ",
            "Stage width");
        // determine max number of stages possible based on width
        int maxStages = image.Width / stageWidth;

        // set up defaults for ease of use
        int defaultStages = 0;
        int defaultHeight = 0;

        if (GetYesNo("Would you like to set a default for the number of stages?"))
        {
            defaultStages = ValidatePositiveInt(
                "Enter the default number of stages: ",
                "Default stages",
                maxStages);
        }

        if (GetYesNo("Would you like to set a default for the row height?"))
        {
            defaultHeight = ValidatePositiveInt(
                "Enter the default row height in pixels: ",
                "Default height",
                totalHeight);
        }

        string suffix = "";
        bool suffixAccepted = false;

        while (!suffixAccepted)
        {
            if (GetYesNo("Would you like to add a suffix to all file names?"))
            {
                Console.WriteLine("Enter the suffix: ");
                suffix = AssignInputOrCancel();
                Console.WriteLine($"All file names will now end with \"{suffix}\"");
                suffixAccepted = GetYesNo("Is this correct?");
            }
            else
                suffixAccepted = true;
        }



        // let user know the image height (they might notice if it seems off from what they were expecting)
        Console.WriteLine($"\nSprite sheet loaded successfully! Total height: {totalHeight}px");
        Console.WriteLine("Now let's go through each row!\n");

        // tracking variables
        int currentY = 0;
        int rowNumber = 1;

        bool jumpToRow = false;
        while (!jumpToRow)
        {
            if (GetYesNo("Would you like to jump to a specific row? (You will need the Y value of the given row)"))
            {
                rowNumber = ValidatePositiveInt(
                    "Enter the row number to start from: ",
                    "Row number");

                currentY = ValidatePositiveInt(
                    "Enter the Y position to start from: ",
                    "Y position",
                    totalHeight - 1);

                if(GetYesNo($"Jump to row {rowNumber}, starting with Y Position {currentY}?"))
                {
                    jumpToRow = true;
                }
                Console.WriteLine("\n");
            }
            else
            {
                jumpToRow = true;
            }
        }

        // step through each row and get user input to slice
        while (currentY < totalHeight)
        {
            Console.WriteLine($"Row {rowNumber} (starting at Y: {currentY}px)");

            // get number of stages or use default
            int stages;
            if (defaultStages > 0)
            {
                Console.Write($"How many stages does this row have? (or use default of {defaultStages} by pressing enter): ");
                string input = AssignInputOrCancel();
                stages = string.IsNullOrWhiteSpace(input)
                    ? defaultStages
                    : ValidatePositiveInt(
                        $"How many stages does this row have? (or use default of {defaultStages} by pressing enter): ",
                        "Number of stages",
                        maxStages,
                        input);
            }
            else
                stages = ValidatePositiveInt(
                    "How many stages does this row have? ",
                    "Number of stages",
                    maxStages);

            // get row height or use default
            int rowHeight;
            int maxHeight = image.Height - currentY;
            if (defaultHeight > 0)
            {
                Console.Write($"How tall is this row in pixels? (or use default of {defaultHeight}px by pressing enter): ");
                string input = AssignInputOrCancel();
                rowHeight = string.IsNullOrWhiteSpace(input)
                    ? defaultHeight
                    : ValidatePositiveInt(
                        $"How tall is this row in pixels? (or use default of {defaultHeight}px by pressing enter): ",
                        "Row height",
                        maxHeight,
                        input);
            }
            else
                rowHeight = ValidatePositiveInt(
                    "How tall is this row in pixels? ",
                    "Row height",
                    maxHeight);

            string imageName;
            bool validName = false;

            do
            {
                Console.Write("What should this row's image be named? ");
                imageName = AssignInputOrCancel();

                if (string.IsNullOrWhiteSpace(imageName))
                    imageName = $"row_{rowNumber}";

                imageName += suffix;

                string potentialPath = Path.Combine(outputFolder, $"{imageName}.png");

                if (File.Exists(potentialPath))
                {
                    string prompt = $"{imageName}.png already exists. Would you like to overwrite it?";

                    if (GetYesNo(prompt))
                    {
                        validName = true;
                    }
                    else
                    {
                        Console.WriteLine("Please enter a different name.\n");
                    }
                }
                else
                    validName = true;
            } while (!validName);

            // calculate dimensions and slice
            int rowWidth = stages * stageWidth;

            Rectangle rowArea = new Rectangle(0, currentY, rowWidth, rowHeight);

            using Image<Rgba32> cropped = image.Clone(ctx => ctx.Crop(rowArea));

#pragma warning disable CS8604 // Possible null reference argument. Suppressing as the path is previously validated
            string outputPath = Path.Combine(
                outputFolder,
                $"{imageName}.png");
#pragma warning restore CS8604 // Possible null reference argument.

            cropped.SaveAsPng(outputPath);

            Console.WriteLine($"Saved {imageName}.png!\n");

            // update tracking variables
            currentY += rowHeight;
            rowNumber++;
        }

        Console.WriteLine("All rows have been processed!");
        Console.WriteLine("==========================\n");
    }
    catch (CancelException)
    {
        Console.WriteLine("\nProcess cancelled. Starting over.\n");
        Console.WriteLine("==========================\n");
    }
    
} while (GetYesNo("Would you like to start again?", false));

// user is done, say goodbye!
Console.WriteLine("Thanks for using Sprite Slicer! Press any key to exit.");
Console.ReadKey();


// validation function for user input converted to int
static int ValidatePositiveInt(string prompt, string valueName, int max = int.MaxValue, string? initialInput = null)
{
    int result;
    bool isNumber;
    string input = initialInput ?? "";

    if (string.IsNullOrWhiteSpace(input))
    {
        do
        {
            Console.Write(prompt);
            input = AssignInputOrCancel();
            isNumber = int.TryParse(input, out result);

            // validate input - check for non-int entries and 0/negative entries
            if (!isNumber)
                Console.WriteLine("Your entry was invalid. Please enter only whole numbers using digits.");
            else if (result <= 0)
                Console.WriteLine($"{valueName} must be greater than zero.");
            else if (result > max)
                Console.WriteLine($"{valueName} cannot exceed {max}.");
        } while (!isNumber || result <= 0 || result > max);
    }
    else
    {
        isNumber = int.TryParse(input, out result);
        while (!isNumber || result <= 0 || result > max)
        {
            if (!isNumber)
                Console.WriteLine("Your entry was invalid. Please enter only whole numbers using digits.");
            else if (result <= 0)
                Console.WriteLine($"{valueName} must be greater than zero.");
            else if (result > max)
                Console.WriteLine($"{valueName} cannot exceed {max}.");

            Console.Write(prompt);
            input = AssignInputOrCancel();
            isNumber = int.TryParse(input, out result);
        }
    }
    return result;
}

// function to find a folder or create one if it doesn't exist
static string GetOrCreateFolder(string folderName)
{
    // run until a path is returned
    while (true)
    {
        // prompt for user input
        Console.Write($"Enter the {folderName} folder path: ");
        string folder = AssignInputOrCancel().Trim('"');

        // if the folder exists, just return it
        if (Directory.Exists(folder))
            return folder;

        // if the folder doesn't yet exist, ask if the user would like it created
        if (GetYesNo("That folder doesn't exist yet. Would you like to create it?"))
        {
            Directory.CreateDirectory(folder);
            return folder;
        }

        Console.WriteLine("Folder not created.");
        Console.WriteLine("Please try again with a different folder path.");
    }
}

static bool GetYesNo(string prompt, bool cancelable = true)
{
    Console.Write($"{prompt} (y/n) ");
    string response;
    if (cancelable)
        response = AssignInputOrCancel().ToLower();
    else
        response = Console.ReadLine().ToLower();

    while (response != "y" && response != "n")
    {
        Console.WriteLine("Invalid response.");
        Console.Write($"{prompt} (y/n) ");
        if (cancelable)
            response = AssignInputOrCancel().ToLower();
        else
            response = Console.ReadLine().ToLower();
    }

    return response == "y";
}

static string AssignInputOrCancel()
{
    string input = Console.ReadLine();
    if (input.Trim().ToLower() == "cancel")
        throw new CancelException();
    else
        return input;
}


// custom exception to allow user to start over at any point
class CancelException : Exception { }
