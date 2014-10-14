
#include "FBXUtils.h"

#include <assert.h>
#include <vector>
#include <map>

#include <fbxsdk.h>
//#include <fbxfilesdk/kfbxio/kfbxiosettings.h>

FbxManager* gMgr = NULL;
FbxScene* gModel = NULL;
FbxSurfacePhong* gMat = NULL; // "dummy" material

std::string gErrStr = "";

FbxColor getColorFromUInt(unsigned int index)
{
	return FbxColor(
		((double)( index >> 24 & 0xFF )) / 0xFF,
		((double)( index >> 16 & 0xFF )) / 0xFF,
		((double)( index >> 8 & 0xFF )) / 0xFF,
		((double)( index & 0xFF )) / 0xFF
		);
}

bool initFbxSdk()
{
	// Create the FBX SDK manager
	gMgr = FbxManager::Create();

	return true;
}

bool preprocessSourceMesh(FbxMesh* mesh)
{
	// Source mesh must have a layer of vertex data,
	// which must not have vertex colors
	if (mesh->GetLayerCount() <= 0 ||
		mesh->GetLayer(0)->GetLayerElementOfType(FbxLayerElement::eVertexColor) != NULL)
		return false;

	// Initialize color array (for "hiding" original vertex indexes)
	FbxLayerElementVertexColor* colors =  static_cast<FbxLayerElementVertexColor*>(mesh->GetLayer(0)->CreateLayerElementOfType(FbxLayerElement::eVertexColor));
	colors->SetMappingMode(FbxLayerElement::eByControlPoint);
	colors->SetReferenceMode(FbxLayerElement::eDirect);

	// Fill color array
	for (int cpi = 0; cpi < mesh->GetControlPointsCount(); ++cpi)
	{
		colors->GetDirectArray().Add( getColorFromUInt(cpi) );
	}

	return true;
}

FbxMesh* extractMorphTarget(FbxMesh* srcMesh, FbxBlendShapeChannel* mtChannel)
{
	// There needs to be one morph target defined
	if(mtChannel->GetTargetShapeCount() <= 0)
		return NULL;
	FbxShape* mt = mtChannel->GetTargetShape(0);

	// Morph target mesh must have a layer of vertex data
	if(mt->GetLayerCount() <= 0)
		return NULL;

	// Create new morph target mesh
	std::string mt_name = std::string("MT&") + srcMesh->GetNode()->GetName() + "&" + mt->GetName();
	FbxNode* mtnode = FbxNode::Create(gModel, mt_name.c_str());
	FbxMesh* mtmesh = FbxMesh::Create(gModel, mt_name.c_str());
	mtnode->SetNodeAttribute(mtmesh);
	FbxNode* root = gModel->GetRootNode();
	root->AddChild(mtnode);
	mtnode->LclScaling.Set(mtChannel->GetBlendShapeDeformer()->GetGeometry()->GetNode()->LclScaling.Get());

	// Define vertices (actually vertex target positions + indexes):

	// Initialize vertex array
	mtmesh->InitControlPoints(mt->GetControlPointIndicesCount());
	FbxVector4* mtverts = mtmesh->GetControlPoints();

	// Initialize normal array
	FbxGeometryElementNormal* mtnorms = NULL;
	if(mt->GetLayer(0)->GetNormals() != NULL)
	{
		mtnorms = mtmesh->CreateElementNormal();
		mtnorms->SetMappingMode(FbxLayerElement::eByControlPoint);
		mtnorms->SetReferenceMode(FbxLayerElement::eDirect);
	}

	// Initialize color (but really target vertex index) array
	FbxGeometryElementVertexColor* mtcolors =  mtmesh->CreateElementVertexColor();
	mtcolors->SetMappingMode(FbxLayerElement::eByControlPoint);
	mtcolors->SetReferenceMode(FbxLayerElement::eDirect);

	// Set target vertices and normals
	for (int cpi = 0; cpi < mt->GetControlPointIndicesCount(); ++cpi)
	{
		int tvi = mt->GetControlPointIndices()[cpi];

		mtverts[cpi] = mt->GetControlPointAt(tvi);

		if (mtnorms != NULL)
			mtnorms->GetDirectArray().Add(mt->GetLayer(0)->GetNormals()->GetDirectArray()[tvi]);

		// "Hide" affected vertex index in the color channel
		mtcolors->GetDirectArray().Add(getColorFromUInt(tvi));
	}

	// Create "dummy" faces
	int num_faces = mtmesh->GetControlPointsCount()/3;
	for (int fi = 0; fi < num_faces; ++fi)
	{
		mtmesh->BeginPolygon();

		mtmesh->AddPolygon(3*fi);
		mtmesh->AddPolygon(3*fi+1);
		mtmesh->AddPolygon(3*fi+2);

		mtmesh->EndPolygon();
	}

	// Apply "dummy" material
	FbxGeometryElementMaterial* mtmats = mtmesh->CreateElementMaterial();
	mtmats->SetMappingMode(FbxGeometryElement::eByPolygon);
	mtmats->SetReferenceMode(FbxGeometryElement::eIndexToDirect);
	// Add to node
	mtnode->AddMaterial(gMat);
	// Apply to each face
	mtmats->GetIndexArray().SetCount(num_faces);
	for( int fi = 0; fi < num_faces; ++fi )
		mtmats->GetIndexArray().SetAt( fi, 0 );

	mtnode->SetShadingMode(FbxNode::eLightShading);

	return mtmesh;
}

bool importFbxModel(const char* path)
{
#ifdef _DEBUG
	assert( gMgr != NULL );
#endif

	// Set import settings
	FbxIOSettings* ios = FbxIOSettings::Create(gMgr, IOSROOT);
	gMgr->SetIOSettings(ios);

	// Initialize importer
	FbxImporter* importer = FbxImporter::Create( gMgr, "" );
	bool result = importer->Initialize(path, -1, gMgr->GetIOSettings());
	if( !result )
	{
		gErrStr = importer->GetStatus().GetErrorString();
		importer->Destroy();

		return false;
	}

	// Import scene
	gModel = FbxScene::Create(gMgr, "Model");
	importer->Import(gModel);
	importer->Destroy();

	return true;
}

bool preprocessMorphMeshes()
{
#ifdef _DEBUG
	assert( gMgr != NULL );
	assert( gModel != NULL );
#endif

	// Create "dummy" material
	FbxDouble3 black_color(0.0, 0.0, 0.0);
	gMat = FbxSurfacePhong::Create(gModel, "MTMat");
	gMat->Emissive.Set(black_color);
	gMat->Ambient.Set(black_color);
	gMat->Diffuse.Set(black_color);
	gMat->TransparencyFactor.Set(100);
	gMat->ShadingModel.Set("Phong");
	gMat->Shininess.Set(0);

	std::vector<FbxMesh*> meshes;
	std::vector<FbxBlendShape*> morphers;

	// Get all the morphed meshes in the model
	for (int node_i = 0; node_i < gModel->GetNodeCount(); ++node_i)
	{
		FbxNode* node = gModel->GetNode(node_i);
		FbxMesh* mesh = node->GetMesh();
	
		if (mesh == NULL || mesh->GetLayerCount() <= 0)
			continue;

		bool is_morphed = false;
		for (int def_i = 0; def_i < mesh->GetDeformerCount(); ++def_i)
		{
			FbxDeformer* def = mesh->GetDeformer(def_i);
			
			// Does this mesh have morph targets / blend shapes?
			if (def->GetRuntimeClassId() == FbxBlendShape::ClassId)
			{
				is_morphed = true;
				morphers.push_back(static_cast<FbxBlendShape*>(def));

				break;
			}
		}
		
		if (is_morphed)
			meshes.push_back(mesh);
	}

	// Preprocess each mesh
	for (size_t mesh_i = 0; mesh_i < meshes.size(); ++mesh_i)
	{
		FbxMesh* mesh = meshes[mesh_i];
		FbxBlendShape* morpher = morphers[mesh_i];

		preprocessSourceMesh(mesh);

		for (int morph_i = 0; morph_i < morpher->GetBlendShapeChannelCount(); ++morph_i)
		{
			// Prepare morph target mesh
			extractMorphTarget (mesh, morpher->GetBlendShapeChannel(morph_i));
		}

		// Destroy the original morph data
		for (int def_i = 0; def_i < mesh->GetDeformerCount(); ++def_i)
		{
			if (mesh->GetDeformer(def_i) == morpher)
			{
				mesh->RemoveDeformer(def_i);
				morpher->Destroy();

				break;
			}
		}
	}

	return true;
}

bool exportFbxModel(const char* path)
{
#ifdef _DEBUG
	assert( gMgr != NULL );
	assert( gMgr->GetIOSettings() != NULL );
	assert( gModel != NULL );
#endif

	// Initialize exporter
	FbxExporter* exporter = FbxExporter::Create(gMgr, "");
	bool result = exporter->Initialize( path, -1, gMgr->GetIOSettings() );
	// bool result = exporter->Initialize( path, 1, gMgr->GetIOSettings() ); // FBX ASCII format (for debugging)
	if (!result)
	{
		gErrStr = exporter->GetStatus().GetErrorString();
		exporter->Destroy();

		return false;
	}

	// Export scene
	exporter->Export(gModel);
	exporter->Destroy();

	return true;
}

bool shutdownFbxSdk()
{
	gMgr->Destroy();

	return true;
}

const std::string& getErrorStr()
{
	return gErrStr;
}
