using System;
using System.Collections.Generic;
using System.Text;

namespace OsuTypes
{
    public class HitObjectGroup
    {
        //Bounds for how far outside the standard osu playing field notes are allowed to go on transformations
        public static float xLowerBound = -51.2f;
        public static float xUpperBound = 563.2f;
        public static float yLowerBound = -38.4f;
        public static float yUpperBound = 422.4f;

        public List<HitObject> hitObjects;
        public float startX;
        public float startY;
        public float endX;
        public float endY;
        public float minX;
        public float minY;
        public float maxX;
        public float maxY;
        public float length;
        public float minNoteDistance;
        public float maxNoteDistance;

        public HitObjectGroup(List<HitObject> hitObjects)
        {
            this.hitObjects = hitObjects;
            startX = hitObjects[0].x;
            startY = hitObjects[0].y;
            endX = hitObjects[hitObjects.Count - 1].x;
            endY = hitObjects[hitObjects.Count - 1].y;

            minX = maxX = startX;
            minY = maxY = startY;
            length = 0f;
            minNoteDistance = float.PositiveInfinity;
            maxNoteDistance = 0f;
            for (int i = 1; i < hitObjects.Count; i++)
            {
                HitObject currentHitObject = hitObjects[i];
                HitObject prevHitObject = hitObjects[i - 1];

                minX = Math.Min(minX, currentHitObject.x);
                maxX = Math.Max(maxX, currentHitObject.x);
                minY = Math.Min(minY, currentHitObject.y);
                maxY = Math.Max(maxY, currentHitObject.y);

                float noteDistance = OsuUtility.EuclideanDistance(prevHitObject.x, prevHitObject.y, currentHitObject.x, currentHitObject.y);
                length += noteDistance;
                minNoteDistance = Math.Min(minNoteDistance, noteDistance);
                maxNoteDistance = Math.Max(maxNoteDistance, noteDistance);
            }
        }

        public void Scale(float scaleFactor)
        {
            Scale(scaleFactor, startX, startY);
        }

        public void Scale(float scaleFactor, float centerX, float centerY)
        {
            Func<float, float> scaleX = x => (x - centerX) * scaleFactor + centerX;
            Func<float, float> scaleY = y => (y - centerY) * scaleFactor + centerY;

            startX = scaleX(startX);
            startY = scaleY(startY);
            endX = scaleX(endX);
            endY = scaleY(endY);
            minX = scaleX(minX);
            minY = scaleY(minY);
            maxX = scaleX(maxX);
            maxY = scaleY(maxY);
            length *= scaleFactor;
            minNoteDistance *= scaleFactor;
            maxNoteDistance *= scaleFactor;

            foreach (HitObject hitObject in hitObjects)
            {
                hitObject.x = scaleX(hitObject.x);
                hitObject.y = scaleY(hitObject.y);
            }
        }

        public void BoundScale(float scaleFactor)
        {
            BoundScale(scaleFactor, startX, startY);
        }

        public void BoundScale(float scaleFactor, float centerX, float centerY)
        {
            //Find greatest value for scale factor that does not put the stream out of bounds
            if (minX != centerX) scaleFactor = Math.Min(scaleFactor, (xLowerBound - centerX) / (minX - centerX));
            if (maxX != centerX) scaleFactor = Math.Min(scaleFactor, (xUpperBound - centerX) / (maxX - centerX));
            if (minY != centerY) scaleFactor = Math.Min(scaleFactor, (yLowerBound - centerY) / (minY - centerY));
            if (maxY != centerY) scaleFactor = Math.Min(scaleFactor, (yUpperBound - centerY) / (maxY - centerY));

            Scale(scaleFactor, centerX, centerY);
        }

        public void Translate(float xTranslation, float yTranslation)
        {
            startX += xTranslation;
            startY += yTranslation;
            endX += xTranslation;
            endY += yTranslation;
            minX += xTranslation;
            minY += yTranslation;
            maxX += xTranslation;
            maxY += yTranslation;

            foreach (HitObject hitObject in hitObjects)
            {
                hitObject.x += xTranslation;
                hitObject.y += yTranslation;
                hitObject.endX += xTranslation;
                hitObject.endY += yTranslation;
            }
        }

        public void BoundTranslate(float xTranslation, float yTranslation)
        {
            //Find greatest value that can be translated for each x and y without putting group members out of bounds
            if (xTranslation > 0) xTranslation = Math.Min(xTranslation, xUpperBound - maxX);
            if (xTranslation < 0) xTranslation = Math.Max(xTranslation, xLowerBound - minX);
            if (yTranslation > 0) yTranslation = Math.Min(yTranslation, yUpperBound - maxY);
            if (yTranslation < 0) yTranslation = Math.Max(yTranslation, yLowerBound - minY);

            Translate(xTranslation, yTranslation);
        }
    }
}
