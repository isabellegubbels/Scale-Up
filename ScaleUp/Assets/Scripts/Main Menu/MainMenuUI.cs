using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MainMenuUI : MonoBehaviour
{
    [SerializeField] private Button playButton, optionsButton, helpButton;
    [SerializeField] private GameObject optionsPanel, helpPanel;
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioClip clickSFX;

    void PlayButtonSfx()
    {
        if (sfxSource && clickSFX) sfxSource.PlayOneShot(clickSFX);
    }

    public void PlayGame()
    {
        PlayButtonSfx();
        SceneManager.LoadScene("MainScene");
    }

    public void OpenOptions()
    {
        PlayButtonSfx();
        optionsPanel.SetActive(true);
    }

    public void CloseOptions()
    {
        PlayButtonSfx();
        optionsPanel.SetActive(false);
    }

    public void OpenHelp()
    {
        PlayButtonSfx();
        helpPanel.SetActive(true);
    }

    public void CloseHelp()
    {
        PlayButtonSfx();
        helpPanel.SetActive(false);
    }
}
