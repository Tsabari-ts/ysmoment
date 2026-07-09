// Central place for the business's external links, reused by the guest order
// page footer and the marketing landing page — single source of truth so the
// WhatsApp text/number don't drift between the two.

export const WHATSAPP_CONTACT_URL =
  'https://api.whatsapp.com/send?phone=972524225365&text=' +
  encodeURIComponent('היי, אשמח לשמוע פרטים נוספים על שירות הברקוד ולסגור אירוע');

export const BUSINESS_PHONE_DISPLAY = '052-4225365';
export const BUSINESS_PHONE_TEL = 'tel:0524225365';

export const INSTAGRAM_URL = 'https://www.instagram.com/yagel.photographer?igsh=MTdpemd2dDZ5MHg3NQ==';
export const FACEBOOK_URL = 'https://www.facebook.com/share/1ArQB8Jq55/';
// Trimmed to the plain profile URL — the tracking params (fbclid, _aem, etc.)
// in the original shared link are single-use/session-specific and would be
// stale or meaningless embedded site-wide.
export const TIKTOK_URL = 'https://www.tiktok.com/@yagel_photographer';

export const DEVELOPER_CREDIT_URL =
  'https://api.whatsapp.com/send/?phone=972506473564&text=%D7%92%D7%9D+%D7%90%D7%A0%D7%99+%D7%A8%D7%95%D7%A6%D7%94+%D7%90%D7%AA%D7%A8+%D7%9E%D7%93%D7%94%D7%99%D7%9D%21+%D7%90%D7%A9%D7%9E%D7%97+%D7%A9%D7%AA%D7%97%D7%96%D7%95%D7%A8+%D7%90%D7%9C%D7%99+%3A%29&type=phone_number&app_absent=0';
