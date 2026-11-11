export function Field({ label, children }: { label: string; children: React.ReactNode }) {
  return (
    <label className="block min-w-[140px] flex-1">
      <span className="text-xs text-gray-600">{label}</span>
      {children}
    </label>
  );
}
