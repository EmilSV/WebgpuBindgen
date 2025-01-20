param(
    [int32]$chromiumVersion,
    [string]$outputFolder,
    [string]$architecture = "x64"
)

$branchName = "chromium/$chromiumVersion"
$outputFolder = [IO.Path]::GetFullPath($outputFolder)

$osWindows = [System.Runtime.InteropServices.RuntimeInformation]::IsOSPlatform([System.Runtime.InteropServices.OSPlatform]::Windows)
$osMacOS = [System.Runtime.InteropServices.RuntimeInformation]::IsOSPlatform([System.Runtime.InteropServices.OSPlatform]::OSX)
$osLinux = [System.Runtime.InteropServices.RuntimeInformation]::IsOSPlatform([System.Runtime.InteropServices.OSPlatform]::Linux)



# Write-Host "installing dependencies..."
write-host "installing dependencies..."

if ($osWindows) {
    winget install -e --id Git.Git
        winget install -e --id Kitware.CMake
    winget install -e --id Python.Python.3.9
    winget install -e --id LLVM.LLVM
}
elseif (osMacOS) {
    brew install cmake
    brew install python@3.9
    brew install llvm
}


git clone -b $branchName https://dawn.googlesource.com/dawn 

Set-Location dawn

if ($osWindows) {
    cmake `
        -B dawn.build-$architecture `
        -A $architecture `
        -D DAWN_FETCH_DEPENDENCIES=ON `
        -D CMAKE_C_COMPILER=clang `
        -D CMAKE_CXX_COMPILER=clang++ `
        -D CMAKE_BUILD_TYPE=Release `
        -D CMAKE_POLICY_DEFAULT_CMP0091=NEW `
        -D CMAKE_POLICY_DEFAULT_CMP0092=NEW `
        -D CMAKE_MSVC_RUNTIME_LIBRARY=MultiThreaded `
        -D BUILD_SHARED_LIBS=OFF `
        -D BUILD_SAMPLES=OFF `
        -D DAWN_ENABLE_D3D12=ON `
        -D DAWN_ENABLE_D3D11=ON `
        -D DAWN_ENABLE_NULL=OFF `
        -D DAWN_ENABLE_DESKTOP_GL=OFF `
        -D DAWN_ENABLE_OPENGLES=OFF `
        -D DAWN_ENABLE_VULKAN=ON `
        -D DAWN_USE_GLFW=OFF `
        -D DAWN_BUILD_SAMPLES=OFF `
        -D DAWN_ENABLE_SPIRV_VALIDATION=OFF `
        -D DAWN_BUILD_MONOLITHIC_LIBRARY=ON `
        -D TINT_BUILD_TESTS=OFF `
        -D TINT_BUILD_GLSL_VALIDATOR=OFF `

    Set-Variable -Name "CL" -Value "/Wv:18"
    cmake.exe --build dawn.build-x64 --config Release --target webgpu_dawn --parallel
}
elseif ($osMacOS) {

    cmake `
        -B dawn.build-$architecture `
        -A $architecture `
        -D DAWN_FETCH_DEPENDENCIES=ON `
        -D CMAKE_C_COMPILER=clang `
        -D CMAKE_CXX_COMPILER=clang++ `
        -D CMAKE_BUILD_TYPE=Release `
        -D CMAKE_POLICY_DEFAULT_CMP0091=NEW `
        -D CMAKE_POLICY_DEFAULT_CMP0092=NEW `
        -D CMAKE_MSVC_RUNTIME_LIBRARY=MultiThreaded `
        -D BUILD_SHARED_LIBS=OFF `
        -D BUILD_SAMPLES=OFF `
        -D DAWN_ENABLE_NULL=OFF `
        -D DAWN_ENABLE_OPENGLES=OFF `
        -D DAWN_ENABLE_METAL=ON `
        -D DAWN_USE_GLFW=OFF `
        -D DAWN_BUILD_SAMPLES=OFF `
        -D TINT_BUILD_TESTS=OFF

    cmake.exe --build dawn.build-x64 --config Release --target webgpu_dawn --parallel
}

write-host "build finished"

If (!(test-path -PathType container $outputFolder)) {
    write-host "creating output folder"
    New-Item -ItemType Directory -Path $outputFolder
}


write-host "copying files to output folder"
if ($osWindows) {

    #copy header to output folder"
    Copy-Item .\dawn.build-$architecture\gen\include\dawn\webgpu.h "$outputFolder\webgpu.h"
    #copy lib to output folder"
    Copy-Item .\dawn.build-$architecture\src\dawn\native\Release\webgpu_dawn.lib "$outputFolder\webgpu_dawn.lib"
    #copy dll to output folder"
    Copy-Item .\dawn.build-$architecture\Release\webgpu_dawn.dll "$outputFolder\webgpu_dawn.dll"

    Set-Location ..

}
elseif ($osMacOS) {
    #copy header to output folder"
    Copy-Item .\dawn.build-$architecture\gen\include\dawn\webgpu.h "$outputFolder\webgpu.h"
    #copy lib to output folder"
    Copy-Item .\dawn.build-$architecture\src\dawn\native\Release\libwebgpu_dawn.a "$outputFolder\libwebgpu_dawn.a"
    #copy dylib to output folder"
    Copy-Item .\dawn.build-$architecture\src\dawn\native\Release\libwebgpu_dawn.dylib "$outputFolder\libwebgpu_dawn.dylib"
    
    Set-Location ..
}