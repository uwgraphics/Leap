
#include <iostream>

#include "FBXUtils.h"

int main( int argc, char* argv[] )
{
	if( argc <= 1 )
	{
		std::cout << "Error: FBX file not specified." << std::endl;
		return 0;
	}

	if( !initFbxSdk() )
	{
		std::cout << "Error: Failed to initialize FBX SDK." << std::endl;
		return 0;
	}

	if( !importFbxModel( argv[1] ) )
	{
		std::cout << "Error: Failed to import FBX model." << std::endl;
		std::cout << getErrorStr().c_str() << std::endl;
	}

	if( !preprocessMorphMeshes() )
	{
		std::cout << "Error: Failed to preprocess FBX model." << std::endl;
		std::cout << getErrorStr().c_str() << std::endl;

		return 0;
	}

	if( !exportFbxModel( argv[1] ) )
	{
		std::cout << "Error: Failed to export FBX model." << std::endl;
		std::cout << getErrorStr().c_str() << std::endl;
	}

	shutdownFbxSdk();

	return 0;
}
