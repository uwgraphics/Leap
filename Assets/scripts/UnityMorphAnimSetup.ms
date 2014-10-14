
global mcMappingsDAZGenesis2 = \
#( #( "MouthOpen_body", "ModifierMouthOpen" ),
	#( "MouthCornerUpDown_body", "ModifierLipCornerUpLeft" ),
	--#( "MouthCornerUpDown_body", "ModifierLipCornerUpRight" ), -- Create manually...
	--#( "-MouthCornerUpDown_body", "ModifierLipCornerDownLeft" ), -- Create manually...
	--#( "-MouthCornerUpDown_body", "ModifierLipCornerDownRight" ), -- Create manually...
	#( "LipTopUpL_body", "ModifierLipTopUpLeft" ),
	#( "LipTopUpR_body", "ModifierLipTopUpRight" ),
	--#( "-LipTopUpL_body", "ModifierLipTopDownLeft" ), -- Create manually...
	--#( "-LipTopUpR_body", "ModifierLipTopDownRight" ), -- Create manually...
	#( "MouthNarrowL_body", "ModifierLipCornerNarrowLeft" ),
	#( "MouthNarrowR_body", "ModifierLipCornerNarrowRight" ),
	#( "-MouthNarrowL_body", "ModifierLipCornerStretchLeft" ),
	#( "-MouthNarrowR_body", "ModifierLipCornerStretchRight" ),
	#( "LipBottomDownL_body", "ModifierLipBottomDownLeft" ),
	#( "LipBottomDownR_body", "ModifierLipBottomDownRight" ),
	#( "LipBottomUpL_body", "ModifierLipBottomUpLeft" ),
	#( "LipBottomUpR_body", "ModifierLipBottomUpRight" ),
	#( "JawSideL_body", "ModifierJawLeft" ),
	#( "JawSideR_body", "ModifierJawRight" ),
	#( "EyeLidsTopDownL_body", "ModifierEyelidTopDownLeft" ),
	#( "EyeLidsTopDownR_body", "ModifierEyelidTopDownRight" ),
	#( "EyeLidsTopUpL_body", "ModifierEyelidTopUpLeft" ),
	#( "EyeLidsTopUpR_body", "ModifierEyelidTopUpRight" ),
	#( "EyeLidsBottomUpL_body", "ModifierEyelidBottomUpLeft" ),
	#( "EyeLidsBottomUpR_body", "ModifierEyelidBottomUpRight" ),
	#( "EyeLidsBottomDownL_body", "ModifierEyelidBottomDownLeft" ),
	#( "EyeLidsBottomDownR_body", "ModifierEyelidBottomDownRight" ),
	#( "BrowUpL_body", "ModifierBrowUpLeft" ),
	#( "BrowUpR_body", "ModifierBrowUpRight" ),
	#( "BrowDownL_body", "ModifierBrowDownLeft" ),
	#( "BrowDownR_body", "ModifierBrowDownRight" ),
	#( "BrowSqueeze_body", "ModifierBrowSqueeze" ),
	--#( "head", "HeadPitch" ), -- Create manually...
	--#( "head", "HeadYaw" ), -- Create manually...
	--#( "head", "HeadRoll" ) ) -- Create manually...
	#( "CheekCreaseL_body", "ModifierCheekUpLeft" ),
	#( "CheekCreaseR_body", "ModifierCheekUpRight" ) )

global mcMappingsDAZGenesis = \
#( #( "Angry_body", "ExpressionAnger" ),
	#( "LipTopUpL_body", "ExpressionDisgust" ),
	#( "Fear_body", "ExpressionFear" ),
	#( "MouthFrown_body", "ExpressionSad" ),
	#( "MouthSmile_body", "ExpressionSmileClosed" ),
	#( "MouthSmileOpen_body", "ExpressionSmileOpen" ),
	#( "MouthOpen_body", "ExpressionSurprise" ),
	#( "EyesClosedL_body", "ModifierBlinkLeft" ),
	#( "EyesClosedR_body", "ModifierBlinkRight" ),
	#( "BrowDownL_body", "ModifierBrowDownLeft" ),
	#( "BrowDownR_body", "ModifierBrowDownRight" ),
	#( "BrowSqueeze_body", "ModifierBrowSqueeze" ),
	#( "BrowUpL_body", "ModifierBrowUpLeft" ),
	#( "BrowUpR_body", "ModifierBrowUpRight" ),
	#( "EyesSquintL_body", "ModifierEyeSquintLeft" ),
	#( "EyesSquintR_body", "ModifierEyeSquintRight" ),
	#( "AA_body", "VisemeAah" ),
	#( "M_body", "VisemeBMP" ),
	#( "SH_body", "VisemeChJSh" ),
	#( "T_body", "VisemeDST" ),
	#( "EH_body", "VisemeEh" ),
	#( "ER_body", "VisemeEr" ),
	#( "F_body", "VisemeFV" ),
	#( "IH_body", "VisemeIh" ),
	#( "K_body", "VisemeKG" ),
	#( "L_body", "VisemeN" ),
	#( "OW_body", "VisemeOh" ),
	#( "UW_body", "VisemeR" ),
	#( "TH_body", "VisemeTh" ),
	#( "W_body", "VisemeW" ) )
	
global mcMappingsDAZToon = \
#( #( "EyeClosedL", "ModifierBlinkLeft" ),
	#( "EyeClosedR", "ModifierBlinkRight" ),
	#( "BrowDownL", "ModifierBrowDownLeft" ),
	#( "BrowDownR", "ModifierBrowDownRight" ),
	#( "BrowUpL", "ModifierBrowUpLeft" ),
	#( "BrowUpR", "ModifierBrowUpRight" ),
	#( "MouthSmile", "ExpressionSmileClosed" ),
	#( "MouthGrin", "ExpressionSmileOpen" ),
	#( "MouthFrown", "ExpressionSad" ),
	#( "OY", "VisemeW" ),
	#( "TH", "VisemeTh" ),
	#( "R", "VisemeR" ),
	#( "AW", "VisemeOh" ),
	#( "IY", "VisemeN" ),
	#( "G", "VisemeKG" ),
	#( "IH", "VisemeIh" ),
	#( "F", "VisemeFV" ),
	#( "B", "VisemeBMP" ),
	#( "GazeDown", "ModifierGazeDown" ),
	#( "GazeDownLeft", "ModifierGazeDownLeft" ),
	#( "GazeDownRight", "ModifierGazeDownRight" ),
	#( "GazeLeft", "ModifierGazeLeft" ),
	#( "GazeRight", "ModifierGazeRight" ),
	#( "GazeUp", "ModifierGazeUp" ),
	#( "GazeUpLeft", "ModifierGazeUpLeft" ),
	#( "GazeUpRight", "ModifierGazeUpRight" ) )

global mcMappingsDAZEmotiGuy = \
#( #( "Angry-head_0", "ExpressionAnger" ),
	#( "Confused-head_0", "ExpressionConfused" ),
	#( "MouthYell-head_0", "ExpressionFear" ),
	#( "Sad-head_0", "ExpressionSad" ),
	#( "Smirk-head_0", "ExpressionSmileClosed" ),
	#( "Smile-head_0", "ExpressionSmileOpen" ),
	#( "Surprise-head_0", "ExpressionSurprise" ),
	#( "Blink_l-head_0", "ModifierBlinkLeft" ),
	#( "Blink_r-head_0", "ModifierBlinkRight" ),
	#( "Brows-Together-head_0", "ModifierBrowSqueeze" ),
	#( "Brows-UpDown_l-head_0", "ModifierBrowUpLeft" ),
	#( "Brows-UpDown_r-head_0", "ModifierBrowUpRight" ),
	#( "MouthSpeak-head_0", "VisemeAh" ),
	#( "MouthYell-head_0", "VisemeAah" ),
	#( "MouthF-head_0", "VisemeBMP" ),
	#( "MouthO-head_0", "VisemeChJSh" ),
	#( "MouthTH-head_0", "VisemeDST" ),
	#( "MouthSpeak-head_0", "VisemeEh" ),
	#( "MouthSpeak-head_0", "VisemeEr" ),
	#( "MouthF-head_0", "VisemeFV" ),
	#( "MouthSpeak-head_0", "VisemeIh" ),
	#( "MouthSpeak-head_0", "VisemeKG" ),
	#( "MouthTH-head_0", "VisemeN" ),
	#( "MouthO-head_0", "VisemeOh" ),
	#( "MouthTH-head_0", "VisemeR" ),
	#( "MouthTH-head_0", "VisemeTh" ),
	#( "MouthO-head_0", "VisemeW" ) )

fn getMorphChannelNameForMT mcNameMappings mtName =
(
	for mci = 1 to mcNameMappings.count do
	(
		if mcNameMappings[mci][1] == mtName then
		(
			return mcNameMappings[mci][2]
		)
	)
	
	return ""
)

fn setupMorphTargets =
(
	for obj in selection do
	(
		for mti = 1 to 100 do
		(
			if ( wm3_mc_hasdata obj.morpher mti ) then
			(
				mt_name = wm3_mc_getname obj.morpher mti
				
				format "Extracting morph target %\n" mt_name
				
				-- Extract each morph target into a separate mesh
				/*wm3_mc_setvalue obj.morpher mti 100.0
				snapshot $ name:(mt_name)
				wm3_mc_setvalue obj.morpher mti 0.0*/
				copy $ isSelected:on name:(mt_name)
				WM3_MC_SetValue $.morpher mti 100.0
				maxOps.CollapseNode $ off

				-- Free up some memory, or we'll crash and burn
				gc();
			)
		)
	)
)

fn setupMorphAnimations mcNameMappings =
(
	for obj in selection do
	(
		for mti = 1 to 100 do
		(
			if ( wm3_mc_hasdata obj.morpher mti ) then
			(
				mt_name = wm3_mc_getname obj.morpher mti
				
				format "Extracting animation for morph target %\n" mt_name
				
				-- Create "dummy" node for animation data
				mtnode_name = ""
				mc_name = getMorphChannelNameForMT mcNameMappings mt_name
				if mcNameMappings.count <= 0 then
				(
					mtnode_name = "MC&" + mt_name
				)
				else
				(
					if mc_name == "" then
					(
						format "Morph target % isn't mapped to any channels, ignoring\n" mt_name
						
						continue
					)
					else
					(
						mtnode_name ="MC&" + mc_name
						
						format "Morph target % is mapped to channel %\n" mt_name mc_name
					)
				)
				mtnode = dummy name:mtnode_name
				-- Extract animation keys and add them to the dummy
				morphctrl = obj.morpher[mti].controller
				for key in morphctrl.keys do
				(
					keyt = key.time
					keyv = key.value/100.0
					if keyv <= 0.0001 then
					(
						keyv = 0.0
					)
					else
					(
						keyv = units.decodeValue ( ( keyv as string ) + "m" ) -- must convert to *meters*
					)
					
					newkey = addNewKey mtnode.pos.controller[1].controller key.time
					newkey.value = keyv
					newkey = addNewKey mtnode.pos.controller[2].controller key.time
					newkey.value = keyv
					newkey = addNewKey mtnode.pos.controller[3].controller key.time
					newkey.value = keyv
				)
			)
		)
	)
)
