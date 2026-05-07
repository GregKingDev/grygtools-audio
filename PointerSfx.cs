using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GrygTools.Audio
{
	[RequireComponent(typeof(Selectable))]
	public class PointerSfx : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
	{
		[Serializable]
		private class UpDownClickSfx
		{
			[SerializeField]
			[Tooltip("Sfx played on click")]
			private SfxConfig m_OnClick = null;
			internal SfxConfig OnClick => m_OnClick;
		
			[SerializeField]
			[Tooltip("Sfx played on pointer down")]
			private SfxConfig m_OnPointerDown = null;
			internal SfxConfig OnPointerDown => m_OnPointerDown;
		
			[SerializeField]
			[Tooltip("Sfx played on pointer up, can be superseded by onClick")]
			private SfxConfig m_OnPointerUp = null;
			internal SfxConfig OnPointerUp => m_OnPointerUp;

			[SerializeField]
			[Tooltip("If true invalid click is played on pointer down")]
			private bool m_PlayInvalidClickOnPointerDown = false;
			internal bool PlayInvalidClickOnPointerDown => m_PlayInvalidClickOnPointerDown;
		
			[SerializeField]
			[Tooltip("Sfx played if UI object is not interactable")]
			private SfxConfig m_OnInvalidClick = null;
			internal SfxConfig OnInvalidClick => m_OnInvalidClick;
		}
		
		[Serializable]
		private class EnterExitSfx
		{
			[SerializeField]
			[Tooltip("Sfx played on pointer enter")]
			private SfxConfig m_OnPointerEnter = null;
			internal SfxConfig OnPointerEnter => m_OnPointerEnter;
		
			[SerializeField]
			[Tooltip("Sfx played on pointer exit")]
			private SfxConfig m_OnPointerExit = null;
			internal SfxConfig OnPointerExit => m_OnPointerExit;
		}
		
		[SerializeField]
		private UpDownClickSfx m_UpDownClickSfx = null;
		
		[SerializeField]
		private EnterExitSfx m_EnterExitSfx = null;
		
		private Selectable m_Selectable = null;

		private void Awake()
		{
			if (m_Selectable == null)
			{
				TryGetComponent(out m_Selectable);
			}
		}

		private void Reset()
		{
			TryGetComponent(out m_Selectable);
		}

		public void OnPointerDown(PointerEventData eventData)
		{
			if (!m_Selectable.interactable)
			{
				if (m_UpDownClickSfx.PlayInvalidClickOnPointerDown)
				{
					m_UpDownClickSfx.OnInvalidClick.PlaySfx(gameObject);
				}
				return;
			}
			m_UpDownClickSfx.OnPointerDown.PlaySfx(gameObject);;
		}

		public void OnPointerUp(PointerEventData eventData)
		{
			if (!m_Selectable.interactable)
			{
				return;
			}
			
			if (m_UpDownClickSfx.OnPointerUp.IsSet() && (!m_UpDownClickSfx.OnClick.IsSet() || !eventData.eligibleForClick))
			{
				m_UpDownClickSfx.OnPointerUp.PlaySfx(gameObject);
			}
		}
		
		public void OnPointerClick(PointerEventData eventData)
		{
			if (!m_Selectable.interactable)
			{
				m_UpDownClickSfx.OnInvalidClick.PlaySfx(gameObject);;
			}
			else
			{
				m_UpDownClickSfx.OnClick.PlaySfx(gameObject);
			}
		}

		public void OnPointerEnter(PointerEventData eventData)
		{
			m_EnterExitSfx.OnPointerEnter.PlaySfx(gameObject);
		}
		
		public void OnPointerExit(PointerEventData eventData)
		{
			m_EnterExitSfx.OnPointerExit.PlaySfx(gameObject);
		}
	}
}