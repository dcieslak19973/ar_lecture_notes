using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Screen: list of all courses. First screen after bootstrap.
/// </summary>
public class CourseListScreen : ScreenBase
{
    [Header("References")]
    [SerializeField] private Transform _listContainer;
    [SerializeField] private GameObject _courseRowPrefab;
    [SerializeField] private Button _addCourseButton;
    [SerializeField] private GameObject _addCoursePanel;
    [SerializeField] private TMP_InputField _nameInput;
    [SerializeField] private TMP_InputField _instructorInput;
    [SerializeField] private TMP_InputField _roomInput;
    [SerializeField] private TMP_InputField _scheduleInput;
    [SerializeField] private Button _confirmAddButton;
    [SerializeField] private Button _cancelAddButton;
    [SerializeField] private Button _settingsButton;

    private ICourseService _courses;

    private void Awake()
    {
        _courses = ServiceLocator.Get<ICourseService>();
        _addCourseButton.onClick.AddListener(() => _addCoursePanel.SetActive(true));
        _cancelAddButton.onClick.AddListener(() => _addCoursePanel.SetActive(false));
        _confirmAddButton.onClick.AddListener(() => RunAsync(CreateCourseAsync, "CreateCourse"));
        _settingsButton.onClick.AddListener(AppNavigator.GoToSettings);
        _addCoursePanel.SetActive(false);
    }

    protected override async Task OnShowAsync()
    {
        await RefreshListAsync();
    }

    private async Task RefreshListAsync()
    {
        foreach (Transform child in _listContainer) Destroy(child.gameObject);

        var list = await _courses.GetAllCoursesAsync();
        foreach (var course in list)
        {
            var row = Instantiate(_courseRowPrefab, _listContainer);
            row.GetComponentInChildren<TMP_Text>().text = course.Name;
            row.GetComponentInChildren<Button>().onClick.AddListener(() =>
                AppNavigator.GoToLectureCapture(course.Id));
        }
    }

    private async Task CreateCourseAsync()
    {
        if (string.IsNullOrWhiteSpace(_nameInput.text)) return;
        await _courses.CreateCourseAsync(
            _nameInput.text.Trim(),
            _instructorInput.text.Trim(),
            _roomInput.text.Trim(),
            _scheduleInput.text.Trim());
        _nameInput.text = _instructorInput.text = _roomInput.text = _scheduleInput.text = "";
        _addCoursePanel.SetActive(false);
        await RefreshListAsync();
    }
}
