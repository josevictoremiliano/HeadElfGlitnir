using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx;
using HarmonyLib;
using UnityEngine;
using System;
using System.IO;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;

namespace HeadElfGlitnir
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency("com.jotunn.jotunn", BepInDependency.DependencyFlags.HardDependency)]
    public class HeadElfGlitnir : BaseUnityPlugin
    {
        public const string PluginGUID = "jotav.grit";
        public const string PluginName = "HelmetElf";
        public const string PluginVersion = "1.1.0";
        private Harmony _harmony;
        public static AssetBundle ArmorBundle;
        public static List<string> headList = new List<string> { "HeadElf" };

        private void Awake()
        {
            PrefabManager.OnVanillaPrefabsAvailable += LoadHelmetElf;

            _harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), PluginGUID);
        }

        private void LoadHelmetElf()
        {
            ArmorBundle = GetAssetBundleFromResources("helmetElf");
            if (ArmorBundle == null)
            {
                Debug.LogError("Failed to load AssetBundle!");
                return;
            }

            headList.ForEach(x => AddItemsWithRenderedIcons(x, recipe.HelmetElfRequirements));

            Debug.Log("<color=green>HelmetElf loaded</color>");
        }

        public void AddItemsWithRenderedIcons(string itemName, RequirementConfig[] requirements, bool setSkill = false)
        {
            try
            {
                // Carregar o prefab
                GameObject helmetElfConstruction = ArmorBundle.LoadAsset<GameObject>("HeadElf");
                if (helmetElfConstruction == null)
                {
                    Debug.LogError($"Failed to load GameObject from AssetBundle.");
                    return;
                }

                Debug.Log($"Successfully loaded GameObject '{helmetElfConstruction.name}' from AssetBundle.");

                // Remover o ItemDrop stub se existir
                ItemDrop oldItemDrop = helmetElfConstruction.GetComponent<ItemDrop>();
                if (oldItemDrop != null)
                {
                    DestroyImmediate(oldItemDrop);
                }

                // Clonar um item existente do jogo para usar como base
                GameObject helmetLeather = PrefabManager.Instance.GetPrefab("HelmetLeather");
                if (helmetLeather == null)
                {
                    Debug.LogError("Failed to find HelmetLeather prefab for reference.");
                    return;
                }

                // Adicionar um novo ItemDrop ao prefab do elfo, copiando os dados do HelmetLeather
                ItemDrop newItemDrop = helmetElfConstruction.AddComponent<ItemDrop>();
                ItemDrop helmetLeatherDrop = helmetLeather.GetComponent<ItemDrop>();
                newItemDrop.m_itemData = helmetLeatherDrop.m_itemData.Clone(); // Copiar dados básicos

                // Configurar o tipo do item diretamente aqui
                newItemDrop.m_itemData.m_shared.m_itemType = ItemDrop.ItemData.ItemType.Helmet;
                newItemDrop.m_itemData.m_shared.m_helmetHideHair = ItemDrop.ItemData.HelmetHairType.Default;

                // Restante da configuração do ItemConfig
                ItemConfig config = new ItemConfig
                {
                    Name = "Elven Ears",
                    Description = "A stylish elven ear headpiece",
                    Amount = 1,
                    CraftingStation = "forge",
                    MinStationLevel = 1,
                    Icons = new[] { helmetLeatherDrop.m_itemData.GetIcon() },
                    Requirements = requirements
                };

                // Criar o CustomItem com o novo ItemDrop
                CustomItem customItem = new CustomItem(helmetElfConstruction, true, config);

                // Adicionar o item ao jogo
                ItemManager.Instance.AddItem(customItem);
                Debug.Log($"Item {itemName} adicionado ao jogo.");
            }
            catch (Exception ex)
            {
                Jotunn.Logger.LogError($"Error while adding item: {ex}");
            }
        }

        public static AssetBundle GetAssetBundleFromResources(string fileName)
        {
            Assembly executingAssembly = Assembly.GetExecutingAssembly();
            string[] resourceNames = executingAssembly.GetManifestResourceNames();

            // Depurando todos os recursos disponíveis
            foreach (var resource in resourceNames)
            {
                Debug.Log($"Resource found: {resource}");
            }

            // Procurando especificamente pelo LogicalName definido no .csproj
            string resourceName = resourceNames.FirstOrDefault(str => str == "HeadElfGlitnir.Resources.helmetElf");

            if (string.IsNullOrEmpty(resourceName))
            {
                Debug.LogError($"Could not find resource with name: HeadElfGlitnir.Resources.helmetElf ");
                return null;
            }

            // Verificar se o AssetBundle já está carregado
            AssetBundle existingBundle = AssetBundle.GetAllLoadedAssetBundles().FirstOrDefault(bundle => bundle.name == fileName);
            if (existingBundle != null)
            {
                Debug.Log($"AssetBundle '{fileName}' já está carregado.");
                return existingBundle;
            }

            using (Stream stream = executingAssembly.GetManifestResourceStream(resourceName))
            {
                return AssetBundle.LoadFromStream(stream);
            }
        }

        [HarmonyPatch]
        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
        }
    }
}
