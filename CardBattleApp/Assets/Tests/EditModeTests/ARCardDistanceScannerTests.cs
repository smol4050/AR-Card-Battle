using NUnit.Framework;
using UnityEngine;

public class ARCardDistanceScannerTests
{
    [Test]
    public void AreCardsCloseEnough_WhenDistanceIsUnderThreshold_ReturnsTrue()
    {
        // Arrange (Preparar)
        GameObject scannerObject = new GameObject();
        ARCardDistanceScanner scanner = scannerObject.AddComponent<ARCardDistanceScanner>();

        // Simulamos dos cartas en el espacio 3D
        GameObject cardA = new GameObject("CardA");
        GameObject cardB = new GameObject("CardB");

        // Colocamos la carta A en el centro (0,0,0) y la B muy cerca (0.3 metros)
        cardA.transform.position = Vector3.zero;
        cardB.transform.position = new Vector3(0.3f, 0, 0);

        // Act (Actuar)
        bool canBattle = scanner.AreCardsCloseEnough(cardA.transform, cardB.transform);

        // Assert (Comprobar)
        Assert.IsTrue(canBattle);
    }

    [Test]
    public void AreCardsCloseEnough_WhenCardsAreFar_ReturnsFalse()
    {
        // Arrange (Preparar)
        GameObject scannerObject = new GameObject();
        ARCardDistanceScanner scanner = scannerObject.AddComponent<ARCardDistanceScanner>();

        GameObject cardA = new GameObject("CardA");
        GameObject cardB = new GameObject("CardB");

        // Colocamos las cartas a 2 metros de distancia (mayor que el límite de 0.5f)
        cardA.transform.position = Vector3.zero;
        cardB.transform.position = new Vector3(2.0f, 0, 0);

        // Act (Actuar)
        bool canBattle = scanner.AreCardsCloseEnough(cardA.transform, cardB.transform);

        // Assert (Comprobar)
        Assert.IsFalse(canBattle);
    }

    [Test]
    public void CalculateDistance_WhenOneCardIsNull_ReturnsMinusOne()
    {
        // Arrange (Preparar)
        GameObject scannerObject = new GameObject();
        ARCardDistanceScanner scanner = scannerObject.AddComponent<ARCardDistanceScanner>();

        GameObject cardA = new GameObject("CardA");
        // Dejamos la carta B intencionalmente nula (no detectada por la cámara)
        Transform cardB = null;

        // Act (Actuar)
        float distance = scanner.CalculateDistance(cardA.transform, cardB);

        // Assert (Comprobar)
        Assert.AreEqual(-1f, distance);
    }
}