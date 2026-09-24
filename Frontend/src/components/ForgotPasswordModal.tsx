export default function ForgotPasswordModal({
  open,
  onClose,
}: {
  open: boolean;
  onClose: () => void;
}) {
  if (!open) return null;

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 px-4">
      <div className="w-full max-w-sm rounded-2xl border border-white/10 bg-[#111527] p-6 shadow-xl">
        <h2 className="text-lg font-semibold text-white">Recuperar contraseña</h2>
        <p className="mt-2 text-sm text-gray-400">
          Esta función todavía está en desarrollo. Por ahora, contacta a un administrador
          para que restablezca tu acceso.
        </p>
        <button
          onClick={onClose}
          className="mt-5 w-full rounded-lg bg-white py-2 text-sm font-medium text-[#0a0e1a] hover:bg-gray-200"
        >
          Entendido
        </button>
      </div>
    </div>
  );
}