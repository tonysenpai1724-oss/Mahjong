using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;

public class IAPManager : Singleton<IAPManager>, IDetailedStoreListener
{
    IStoreController m_StoreController;
    Dictionary<string, string> dicPriceString = new Dictionary<string, string>();

    public void InitializePurchasing()
    {
        ConfigurationBuilder builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());

        //Add products that will be purchasable and indicate its type.
        foreach (var item in DataSystem.Instance.dataIAP.dicConfigs)
        {
            builder.AddProduct(item.Value.packID, ProductType.Consumable);
        }
        UnityPurchasing.Initialize(this, builder);
    }

    public void PurchaseIAP(string packId)
    {
        if (m_StoreController != null)
            m_StoreController.InitiatePurchase(packId);
    }

    public string GetPriceString(string packId)
    {
        if (dicPriceString.ContainsKey(packId))
            return dicPriceString[packId];
        return "";
    }

    public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
    {
        Debug.Log("In-App Purchasing successfully initialized");
        m_StoreController = controller;
        foreach (var item in m_StoreController.products.all)
        {
            dicPriceString.Add(item.definition.id, item.metadata.localizedPriceString);
        }
    }

    public void OnInitializeFailed(InitializationFailureReason error)
    {
        OnInitializeFailed(error, null);
    }

    public void OnInitializeFailed(InitializationFailureReason error, string message)
    {
        var errorMessage = $"Purchasing failed to initialize. Reason: {error}.";

        if (message != null)
        {
            errorMessage += $" More details: {message}";
        }

        Debug.Log(errorMessage);
    }

    public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
    {
        //Retrieve the purchased product
        var product = args.purchasedProduct;

        //Add the purchased product to the players inventory
        OnPurchaseSuccess(product.definition.id, product.definition.storeSpecificId);

        Debug.Log($"Purchase Complete - Product: {product.definition.id}");

        //We return Complete, informing IAP that the processing on our side is done and the transaction can be closed.
        return PurchaseProcessingResult.Complete;
    }

    public void OnPurchaseSuccess(string packID, string transactionId)
    {
        IIAPController.Instance.OnPurchaseSuccess(packID, transactionId);
    }

    public void OnPurchaseFailed(Product product, PurchaseFailureReason failureReason)
    {
        Debug.Log($"Purchase failed - Product: '{product.definition.id}', PurchaseFailureReason: {failureReason}");
    }

    public void OnPurchaseFailed(Product product, PurchaseFailureDescription failureDescription)
    {
        Debug.Log($"Purchase failed - Product: '{product.definition.id}'," +
            $" Purchase failure reason: {failureDescription.reason}," +
            $" Purchase failure details: {failureDescription.message}");
    }
}
