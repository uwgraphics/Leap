
#ifndef __FBXUtils_h__
#define __FBXUtils_h__

#include <string>

#ifndef NONCLIENT_BUILD

#define FBXLIB_PATH "C:\\Program Files\\Autodesk\\FBX\\FBX SDK\\2015.1\\lib\\vs2013\\x86"

#pragma comment(lib, "kernel32")
#pragma comment(lib, "user32")
#pragma comment(lib, "gdi32")
#pragma comment(lib, "winspool")
#pragma comment(lib, "comdlg32")
#pragma comment(lib, "advapi32")
#pragma comment(lib, "shell32")
#pragma comment(lib, "ole32")
#pragma comment(lib, "oleaut32")
#pragma comment(lib, "uuid")
#pragma comment(lib, "odbc32")
#pragma comment(lib, "odbccp32")
#pragma comment(lib, "wininet")
#if defined(_DEBUG)
#pragma comment(lib, FBXLIB_PATH"/debug/libfbxsdk-md.lib")
#else
#pragma comment(lib, FBXLIB_PATH"/release/libfbxsdk-md.lib")
#endif

#endif

bool initFbxSdk();
bool importFbxModel( const char* path );
bool preprocessMorphMeshes();
bool exportFbxModel( const char* path );
bool shutdownFbxSdk();
const std::string& getErrorStr();

#endif // __FBXUtils_h__
