using System.Collections.Generic;
using UnityEngine;

// Los iconos de las teclas y de los botones del mando de Xbox (hechos con los tiles pixel de Assets/Buttons/Tiles), por su
// nombre: "keyboard_space", "xbox_a"... Lo usan los carteles. Lo genera Replica > Crear nivel tutorial.
public class ControlIcons : ScriptableObject
{
    [SerializeField] Sprite[] sprites;

    Dictionary<string, Sprite> byName;

    public Sprite Find(string name)
    {
        if (byName == null)
        {
            byName = new Dictionary<string, Sprite>();
            foreach (Sprite sprite in sprites)
                if (sprite != null) byName[sprite.name] = sprite;
        }
        return byName.TryGetValue(name, out Sprite found) ? found : null;
    }

#if UNITY_EDITOR
    public void Set(Sprite[] all)
    {
        sprites = all;
        byName = null;
    }
#endif
}
