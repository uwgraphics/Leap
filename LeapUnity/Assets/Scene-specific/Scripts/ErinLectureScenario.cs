using UnityEngine;
using System.Collections;

public class ErinLectureScenario : LectureScenario
{
	protected override IEnumerator _Run()
	{	
		//Initialize some variables for the light manipulation in the Audio-only condition
		GameObject mainLight = GameObject.FindGameObjectWithTag("MainLight");
		GameObject spotlight = GameObject.FindGameObjectWithTag("Spotlight");
			
		float numSteps = 40f;
		float maxIntensityPoint = mainLight.light.intensity;
		float maxIntensitySpot = 1f;
		
		mainLight.light.intensity = 0f;
		
		//Initialize head alignment parameters based on the condition
		if (condition == ConditionType.Affiliative) {
			mapAlign = 0f;
			cameraAlign = 1f;
			gazeCtrl.LEye.inMR = gazeCtrl.LEye.outMR = 55f;
			gazeCtrl.REye.inMR = gazeCtrl.REye.outMR = 55f;
		}
		else if (condition == ConditionType.Both) {
			mapAlign = 1f;
			cameraAlign = 1f;
			gazeCtrl.LEye.inMR = gazeCtrl.LEye.outMR = 55f;
			gazeCtrl.REye.inMR = gazeCtrl.REye.outMR = 55f;
		}
		else if (condition == ConditionType.Referential) {
			mapAlign = 1f;
			cameraAlign = 0f;
			gazeCtrl.LEye.inMR = gazeCtrl.LEye.outMR = 45f;
			gazeCtrl.REye.inMR = gazeCtrl.REye.outMR = 45f;
		}
		
		//Get the head started in the correct position
		if (condition == ConditionType.Affiliative	|| condition == ConditionType.Both || condition == ConditionType.Audio) {
			//Start by fully aligning with the camera
			gazeCtrl.Head.align = 1.0f;
			gazeCtrl.GazeAt("MainCamera");
			
			yield return StartCoroutine( WaitForGazeShiftFinished() );
			
			gazeCtrl.GazeAt("MainCamera");
			
			yield return StartCoroutine( WaitForGazeShiftFinished() );
			
			yield return new WaitForSeconds(2f);
		}
		else if (condition == ConditionType.Referential) {
			//Align fully with the map, then gaze at the participant out of the corner of one eye
			gazeCtrl.Head.align = 1f;
			
			gazeCtrl.GazeAt("Map");
			
			yield return StartCoroutine( WaitForGazeShiftFinished() );
			
			gazeCtrl.GazeAt("Map");
			
			yield return StartCoroutine( WaitForGazeShiftFinished() );
			
			yield return new WaitForSeconds(2f);
			
			gazeCtrl.Head.align = 0f;
			
			gazeCtrl.GazeAt("MainCamera");
			
			yield return StartCoroutine( WaitForGazeShiftFinished() );
			
			yield return new WaitForSeconds(2f);
		}
		
		mainLight.light.intensity = maxIntensityPoint;
		
		// Start 1. paragraph
		speechCtrl.Speak("ErinLecture1");
		
		if (condition == ConditionType.Audio) {
			//Switch the lights to the Audio condition
			
			yield return new WaitForSeconds(2f);
			
			float i = 0f;
			while (i < numSteps) {
				
				mainLight.light.intensity -= maxIntensityPoint/numSteps;
				spotlight.light.intensity += maxIntensitySpot/numSteps;
			
				yield return new WaitForSeconds(1f/numSteps);
				i += 1f;
			}
			
			mainLight.light.intensity = 0f;
			spotlight.light.intensity = maxIntensitySpot;
		}
		
		yield return StartCoroutine( WaitForSpeechFinished() );
		
		expressionCtrl.changeExpression = true;
		yield return new WaitForSeconds(1f);				
		
		// Start 2. paragraph
		speechCtrl.Speak("ErinLecture2");
		
		// Set parameters of the 1. gaze shift
		gazeCtrl.Head.align = mapAlign;
		
		yield return StartCoroutine( WaitForGazeShiftFinished() );
		
		// 1. gaze shift back
		gazeCtrl.Head.align = cameraAlign;
		
		yield return StartCoroutine( WaitForGazeShiftFinished() );
		
		// Set parameters of the 2. gaze shift
		gazeCtrl.Head.align = mapAlign;
		
		yield return StartCoroutine( WaitForGazeShiftFinished() );
		
		// 2. gaze shift back
		gazeCtrl.Head.align = cameraAlign;
		
		yield return StartCoroutine( WaitForGazeShiftFinished() );
		
		// Set parameters of the 3. gaze shift
		gazeCtrl.Head.align = mapAlign;
		
		yield return StartCoroutine( WaitForGazeShiftFinished() );
		
		// 3. gaze shift back
		gazeCtrl.Head.align = cameraAlign;
		
		yield return StartCoroutine( WaitForGazeShiftFinished() );
		
		// Set parameters of the 4. gaze shift
		gazeCtrl.Head.align = mapAlign;
		
		yield return StartCoroutine( WaitForGazeShiftFinished() );
		
		// 4. gaze shift back
		gazeCtrl.Head.align = cameraAlign;
		
		yield return StartCoroutine( WaitForGazeShiftFinished() );
		
		// Set parameters of the 5. gaze shift
		gazeCtrl.Head.align = mapAlign;
		
		yield return StartCoroutine( WaitForGazeShiftFinished() );
		
		// 5. gaze shift back
		gazeCtrl.Head.align = cameraAlign;
		
		yield return StartCoroutine( WaitForGazeShiftFinished() );
		
		yield return StartCoroutine( WaitForSpeechFinished() );
		
		expressionCtrl.changeExpression = true;
		yield return new WaitForSeconds(1f);				
		// Start 3. paragraph
		speechCtrl.Speak("ErinLecture3");
		
		// Set parameters of the 6. gaze shift
		gazeCtrl.Head.align = mapAlign;
		
		yield return StartCoroutine( WaitForGazeShiftFinished() );
		
		// 6. gaze shift back
		gazeCtrl.Head.align = cameraAlign;
		
		yield return StartCoroutine( WaitForGazeShiftFinished() );
		
		// Set parameters of the 7. gaze shift
		gazeCtrl.Head.align = mapAlign;
		
		yield return StartCoroutine( WaitForGazeShiftFinished() );
		
		// 7. gaze shift back
		gazeCtrl.Head.align = cameraAlign;
		
		yield return StartCoroutine( WaitForGazeShiftFinished() );
		
		// Set parameters of the 8. gaze shift
		gazeCtrl.Head.align = mapAlign;
		
		yield return StartCoroutine( WaitForGazeShiftFinished() );
		
		// 8. gaze shift back
		gazeCtrl.Head.align = cameraAlign;
		
		yield return StartCoroutine( WaitForGazeShiftFinished() );
		
		// Set parameters of the 9. gaze shift
		gazeCtrl.Head.align = mapAlign;
		
		yield return StartCoroutine( WaitForSpeechFinished() );
						
		// Start 4. paragraph
		speechCtrl.Speak("ErinLecture4");
		
		yield return StartCoroutine( WaitForGazeShiftFinished() );
		
		// 9. gaze shift back
		gazeCtrl.Head.align = cameraAlign;
		
		yield return StartCoroutine( WaitForGazeShiftFinished() );
		
		// Set parameters of the 10. gaze shift
		gazeCtrl.Head.align = mapAlign;
		
		yield return StartCoroutine( WaitForGazeShiftFinished() );
		
		// 10. gaze shift back
		gazeCtrl.Head.align = cameraAlign;
		
		yield return StartCoroutine( WaitForGazeShiftFinished() );
		
		yield return StartCoroutine( WaitForSpeechFinished() );
					
		// Start 5. paragraph
		speechCtrl.Speak("ErinLecture5");
		
		// Set parameters of the 11. gaze shift
		gazeCtrl.Head.align = mapAlign;
		
		yield return StartCoroutine( WaitForGazeShiftFinished() );
		
		// 11. gaze shift back
		gazeCtrl.Head.align = cameraAlign;
		
		yield return StartCoroutine( WaitForGazeShiftFinished() );
		
		yield return StartCoroutine( WaitForSpeechFinished() );
		
		expressionCtrl.changeExpression = true;
		yield return new WaitForSeconds(1f);				
		// Start End. paragraph
		speechCtrl.Speak("ErinLectureEnd");
		
		if (condition == ConditionType.Audio) {
			//Switch the lights to the Audio condition
			
			float i = 0f;
			while (i < numSteps) {
				
				mainLight.light.intensity += maxIntensityPoint/numSteps;
				spotlight.light.intensity -= maxIntensitySpot/numSteps;
			
				yield return new WaitForSeconds(1f/numSteps);
				i += 1f;
			}
			
			mainLight.light.intensity = maxIntensityPoint;
			spotlight.light.intensity = 0f;
		}
		
		yield return StartCoroutine( WaitForSpeechFinished() );
		
		expressionCtrl.changeExpression = true;
		yield return new WaitForSeconds(1f);		
		
		// No more gaze shifts, we're done here
	}
	
	protected override void _Finish()
	{
	}
}
