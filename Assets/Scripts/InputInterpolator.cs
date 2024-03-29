﻿using UnityEngine;

public class InputInterpolator
{
    private TransferEngine TransferEngine;
    private Rakel Rakel;
    private Canvas_ Canvas;

    private Vector3 PreviousRakelPosition;
    private float PreviousRakelRotation;
    private float PreviousRakelTilt;

    private float NO_ANGLE = float.NaN;
    private Vector3 NO_POSITION = Vector3.negativeInfinity;

    public InputInterpolator(TransferEngine transferEngine, Rakel rakel, Canvas_ canvas)
    {
        TransferEngine = transferEngine;
        Rakel = rakel;
        Canvas = canvas;
    }

    public void NewStroke()
    {
        PreviousRakelPosition = NO_POSITION;
        PreviousRakelRotation = NO_ANGLE;
        PreviousRakelTilt = NO_ANGLE;
    }

    public void AddNode(Vector3 rakelPosition, float rakelRotation, float rakelTilt, TransferConfiguration transferConfiguration, int interpolationResolution)
    {
        // only reapply if there are changes
        if (!rakelPosition.Equals(PreviousRakelPosition)
            || !rakelRotation.Equals(PreviousRakelRotation)
            || !rakelTilt.Equals(PreviousRakelTilt))
        {
            //Stopwatch sw = new Stopwatch();
            //sw.Start();
            bool isFirstNodeOfStroke = PreviousRakelPosition.Equals(NO_POSITION)
                && PreviousRakelRotation.Equals(NO_ANGLE)
                && PreviousRakelTilt.Equals(NO_ANGLE);
            if (isFirstNodeOfStroke)
            {
                TransferEngine.SimulateStep(
                    rakelPosition,
                    rakelRotation,
                    rakelTilt,
                    transferConfiguration,
                    Rakel,
                    Canvas
                );
            }
            else
            {
                // 1. determine differences and steps
                Vector3 dp = rakelPosition - PreviousRakelPosition;
                //float dpLength = dp.magnitude;
                Vector2 dp_ = Canvas.WorldSpaceCanvas.MapToPixel(rakelPosition) - Canvas.WorldSpaceCanvas.MapToPixel(PreviousRakelPosition);
                float dpLength = dp_.magnitude;
                int positionSteps = (int)(dpLength * interpolationResolution); // don't add 1 because the first one is already done when isFirstNodeOfStroke

                float dr = rakelRotation - PreviousRakelRotation;
                if (Mathf.Abs(dr) >= 300){
                    if (rakelRotation < PreviousRakelRotation) {
                        // turn over case 1: from 360 to 0
                        // -> dr in in this case is something like -345
                        // -> needs to be positive and small though because we want to rotate further over
                        dr = 360 + dr;
                    } else {
                        // turn over case 2: from 0 to 360
                        // -> dr in this case is something like 345
                        // -> needs to be negative negative and small though because we want to rotate further over
                        dr = dr - 360;
                    }
                }
                float arcLength = Mathf.PI * (Rakel.Length / 2) * (Mathf.Abs(dr)/180);
                int rotationSteps = (int)(arcLength * interpolationResolution);

                float dt = Mathf.Abs(PreviousRakelTilt - rakelTilt);
                arcLength = Mathf.PI * Rakel.Width * (Mathf.Abs(dt)/180);
                int tiltSteps = (int)(arcLength * interpolationResolution);

                int steps = Mathf.Max(1, Mathf.Max(Mathf.Max(positionSteps, rotationSteps), tiltSteps))*10;


                // 2. interpolate
                Vector3 previousPosition = PreviousRakelPosition;
                float previousRotation = PreviousRakelRotation;
                float previousTilt = PreviousRakelTilt;

                for (int i=0; i<steps; i++)
                {
                    // first one is skipped, because that was already done when isFirstNodeOfStroke

                    Vector3 currentPosition = previousPosition + dp / steps;
                    //Vector3 currentPosition = PreviousRakelPosition + (i+1) * (dp / steps); // doesn't seem to make a difference

                    float currentRotation = previousRotation + dr / steps;
                    if (currentRotation >= 360) { // fix turnover case 1
                        currentRotation = currentRotation % 360;
                    }
                    if (currentRotation < 0) { // fix turnover case 2
                        currentRotation = 360 + currentRotation;
                    }

                    float currentTilt = previousTilt + dt / steps;

                    TransferEngine.SimulateStep(
                        currentPosition,
                        currentRotation,
                        currentTilt,
                        transferConfiguration,
                        Rakel,
                        Canvas
                    );

                    previousPosition = currentPosition;
                    previousRotation = currentRotation;
                    previousTilt = currentTilt;
                }
            }

            PreviousRakelPosition = rakelPosition;
            PreviousRakelRotation = rakelRotation;
            PreviousRakelTilt = rakelTilt;
            //if (logTime)
            //    UnityEngine.Debug.Log("UpdatePosition took " + sw.ElapsedMilliseconds + "ms");
        }
    }
}
