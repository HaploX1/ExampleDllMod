# ExampleDllMod
This is an example mod to provide an introduction to the RimWorld assembly (.dll) modding part

Excerpt from the forum entry:
(https://ludeon.com/forums/index.php?topic=3408.0)

> # How to make an .dll-mod
> 
> ## Description
> This is a small demonstration about how you can make a mod that's using an assembly (.dll).
> With this little project you learn about the assembly and it's program. Additionally you'll build your own small dark matter generator and an animated wind turbine. :)
> Note, that this is Windows only, as I have no experience with Linux or Mac. Sorry..
> 
> ## Needed Tools
> You need the following tools: 
> - Microsoft Visual Studio 2015 Community Edition (free, download [here](https://www.visualstudio.com/en-us/products/visual-studio-community-vs.aspx))
> - A basic understanding of C#
> - RimWorld
>
> ## Download
> Download the project [from Git-Hub](https://github.com/HaploX1/ExampleDllMod)
>
> ## Preparations
> Before you start the project, you need to make the following preparations:
> - Extract the file to a folder of your choice
> - Go into your RimWorld-Folder:
>     - Copy `..\RimWorld000Win\RimWorld000Win_Data\Managed\Assembly-CSharp.dll`
>       to `..\YourProjectFolder\RimWorld_ExampleProjectDLL\Source-DLLs\`
>     - Copy `..\RimWorld000Win\RimWorld000Win_Data\Managed\UnityEngine.dll`
>       to `..\YourProjectFolder\RimWorld_ExampleProjectDLL\Source-DLLs\`
>
> ## Enter The Project
> Now that you've prepared everything, it is time to enter the project and take a look into what makes the pump do, what it does and where the problem is..
>
> So open the project by starting `<YourExtractionFolder>\RimWorld_ExampleProjectDLL.sln`
>
> If everything is installed correctly, Visual Studio should startup, opening your project.
> Everything else is described inside. 
> And once you're finished, you have created your very own dark matter generator and maybe an animated wind turbine.
>
> With this in mind.. Have fun exploring the secrets of the RimWorld code.  8)
>
> See you on the other side,
> Haplo
